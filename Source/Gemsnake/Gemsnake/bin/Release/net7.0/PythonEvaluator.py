import socket
import json
import sys
import time

class Evaluator:
    EndOfLengthByte = 255
    ReadFrequency = 60  # times per second
    MessageEncoding = "utf-8"

    client_socket = None
    local_variables_from_exec = {}

    def __init__(self, port):
        self.port = port

    def listen(self):
        # Initialize variables
        server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        server.bind(("localhost", self.port))
        server.listen(1)

        Evaluator.client_socket, address = server.accept()

        current_bytes = bytearray()

        # Read messages while connected
        while True:
            # Read all the available data
            try:
                # Try to read data from the client socket in a non-blocking way
                data = Evaluator.client_socket.recv(1024)
                current_bytes.extend(data)
            except BlockingIOError:
                # No data available
                pass
            except Exception:
                # Client closed the connection
                Evaluator.client_socket.close()
                break

            # Check for a complete message in the data
            while len(current_bytes) > 0:
                got_completed_message = False
                for i, byte in enumerate(current_bytes):
                    # Find end of message length byte
                    if byte == Evaluator.EndOfLengthByte:
                        # Get the message length
                        message_length = int.from_bytes(current_bytes[:i], byteorder='big')
                        # Check if the message is complete
                        if len(current_bytes) >= i + 1 + message_length:
                            # Take the message
                            message_bytes = current_bytes[i + 1 : i + 1 + message_length]
                            current_bytes = current_bytes[i + 1 + message_length :]
                            message = Evaluator.bytes_to_string(message_bytes)
                            # Handle the complete message
                            self.respond(message)
                            # Mark got completed message as true
                            got_completed_message = True
                        # Break (the end message length byte has been reached)
                        break
                if not got_completed_message:
                    break

            # Wait until the next read
            time.sleep(1.0 / Evaluator.ReadFrequency)

    def bytes_to_string(bytes):
        return bytes.decode(Evaluator.MessageEncoding)

    def create_packet(self, message):
        # Get message as bytes
        bytes_message = message.encode(Evaluator.MessageEncoding)
        # Insert message length bytes
        message_length_bytes = bytearray()
        byte_count = str(len(bytes_message))
        byte_count_digits = ""
        # Add the digits of the message length as bytes (e.g 670 becomes {67, 0})
        for byte_count_digit in byte_count:
            if not byte_count_digits:
                message_length_bytes.append(int(byte_count_digit))
            else:
                result = int(byte_count_digits + byte_count_digit)
                if result == Evaluator.EndOfLengthByte or len(str(result)) != len(byte_count_digits) + 1:
                    break
                message_length_bytes.append(int(byte_count_digit))
            byte_count_digits += byte_count_digit
        # Add the end of message length byte
        if byte_count_digits:
            message_length_bytes.append(Evaluator.EndOfLengthByte)
        # Build the packet
        packet = message_length_bytes + bytes_message
        return packet

    class EvaluationResponse:
        def __init__(self, error_message, error_line, result):
            self.error_message = error_message
            self.error_line = error_line
            self.result = result

        def to_json(self):
            return {"error_message": self.error_message, "error_line": self.error_line, "result": self.result}

    def respond(self, message):
        # Get the parts of the message (guid:code)
        message_parts = message.split(":", 1)
        guid = message_parts[0]
        code = message_parts[1]
        # Evaluate the code in the message, catching errors
        error_message = None
        error_line = None
        def evaluate_code(code):
            nonlocal error_message, error_line
            try:
                if code.isspace():
                    code = "pass"
                indent = "    "
                code = code.replace("\n", "\n" + indent)
                code = f"""def __python_evaluator_execute_code():
{indent}{code}"""
                exec(code, globals(), Evaluator.local_variables_from_exec)
                return Evaluator.local_variables_from_exec["__python_evaluator_execute_code"]()
            except SyntaxError as ex:
                error_message = str(ex)
                error_line = int(ex.lineno) - 1 if hasattr(ex, "lineno") else None
                return None
            except Exception as ex:
                error_message = str(ex)
                return None
        result = evaluate_code(code)
        # Create a response
        response = Evaluator.EvaluationResponse(error_message, error_line, result)
        # Create a response message from the response
        response_message = json.dumps(response.to_json())
        response_message = guid + ":" + response_message
        # Send the response
        Evaluator.client_socket.sendall(self.create_packet(response_message))

if __name__ == "__main__":
    port = int(sys.argv[1])

    # Start listening
    evaluator = Evaluator(port)
    evaluator.listen()
