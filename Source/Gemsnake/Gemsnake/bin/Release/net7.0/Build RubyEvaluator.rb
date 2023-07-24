# Get strings
Script_Path = "RubyEvaluator.rb"
Executable_Path = Script_Path.gsub(".rb", ".exe")
Modified_Script_Path = "Modified" + Script_Path
Modified_Executable_Path = Modified_Script_Path.gsub(".rb", ".exe")

# Add extra gem requires
puts("To build RubyEvaluator, you will need the following gems installed: ocran, socket, json.")
puts("Enter any extra gems you want to include in a comma-separated list:")
Extra_Gems = gets.strip.split(",")

require_extra_gems = ""
for gem in Extra_Gems do
  unless gem.strip.empty? # Don't require whitespace
    require_extra_gems += "require \"" + gem + "\"\n"

    begin
      require gem
    rescue LoadError
      puts("The gem \"#{gem}\" is not installed.")
      gets
      exit
    end
  end
end

File.write(Modified_Script_Path, require_extra_gems + File.read(Script_Path))

# Create executable
system("ocran \"" + Modified_Script_Path + "\"")

# Clean up
File.rename(Modified_Executable_Path, Executable_Path)
File.delete(Modified_Script_Path)

# Finished
puts("Finished.")
gets