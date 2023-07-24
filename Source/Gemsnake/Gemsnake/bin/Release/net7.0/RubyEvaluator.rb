require "socket"
require "json"

exit if defined? Ocran

class Evaluator
  EndOfLengthByte = 255
  ReadFrequency = 60 # times per second
  MessageEncoding = Encoding::UTF_8
  Port = ARGV[0].to_i

  def listen
    # Initialise variables
    server = TCPServer.new("localhost", Port)

    @client_socket = server.accept

    current_bytes = []

    # Read messages while connected
    loop do
      # Read all the available data
      begin
        # Try to read data from the client socket in a non-blocking way
        data = @client_socket.read_nonblock(1024)
        current_bytes.concat(data.bytes)
      rescue IO::WaitReadable
        # No data available
      rescue
        # Client closed the connection
        @client_socket.close
        break
      end

      # Check for a complete message in the data
      while current_bytes.length > 0
        got_completed_message = false
        current_bytes.each_index do |i|
          # Find end of message length byte
          if current_bytes[i] == EndOfLengthByte then
            # Get the message length
            message_length = current_bytes[0...i].join.to_i
            # Check if the message is complete
            if current_bytes.length >= (i + 1 + message_length) then
              # Take the message
              message_bytes = current_bytes[(i + 1)...(i + 1 + message_length)]
              current_bytes = current_bytes[(i + 1 + message_length)..]
              message = bytes_to_string(message_bytes)
              # Handle the complete message
              respond(message)
              # Mark got completed message as true
              got_completed_message = true
            end
            # Break (the end message length byte has been reached)
            break
          end
        end
        break if got_completed_message == false
      end

      # Wait until the next read
      sleep(1.0 / ReadFrequency)
    end
  end
  def bytes_to_string(bytes)
    bytes.map { |byte| byte.chr(MessageEncoding) }.join
  end

  def create_packet(message)
    # Get message as bytes
    bytes = message.encode("UTF-8").bytes
    # Insert message length bytes
    message_length_bytes = []
    byte_count = bytes.length.to_s
    byte_count_digits = ""
    # Add the digits of the message length as bytes (e.g 670 becomes {67, 0})
    byte_count.each_char do |byte_count_digit|
      if byte_count_digits.empty?
        message_length_bytes << byte_count_digit.to_i
      else
        result = (byte_count_digits + byte_count_digit).to_i
        break if result == EndOfLengthByte || result.to_s.length != byte_count_digits.length + 1
        message_length_bytes << byte_count_digit.to_i
      end
      byte_count_digits += byte_count_digit
    end
    # Add the end of message length byte
    message_length_bytes << EndOfLengthByte if !byte_count_digits.empty?
    # Build the packet
    packet = message_length_bytes + bytes
    return packet
  end

  class EvaluationResponse
    @error_message = nil
    @error_line = nil
    @result = nil

    def initialize(error_message, error_line, result)
      @error_message = error_message
      @error_line = error_line
      @result = result
    end

    def to_json
        {"error_message" => @error_message, "error_line" => @error_line, "result" => @result}.to_json
    end
  end

  def respond(message)
    # Get the parts of the message (guid:code)
    message_parts = message.split(":", 2)
    guid = message_parts[0]
    code = message_parts[1]
    # Evaluate the code in the message, catching returns and errors
    error_message = nil
    error_line = nil
    result = nil
    result = catch(:eval_result) do
      code_lambda = -> { eval(code) }
      value = code_lambda.call
      throw(:eval_result, value)
    rescue SyntaxError, StandardError => ex
      error_message = ex.message
      error_line = ex.backtrace.first.split(":")[1].to_i rescue nil
      nil
    end
    # Create a response
    response = EvaluationResponse.new(error_message, error_line, result)
    # Create a response message from the response
    response_message = response.to_json
    response_message = guid + ":" + response_message
    # Send the response
    @client_socket.write(create_packet(response_message).pack('C*'))
  end
end

# Start listening
evaluator = Evaluator.new
evaluator.listen