import PyInstaller.__main__
import shutil
import os
import importlib

def delete_folder(path):
    shutil.rmtree(path)
def delete_file(path):
    os.remove(path)

# Get strings
base_name = "PythonEvaluator"
modified_base_name = "Modified" + base_name
script_name = base_name + ".py"
modified_script_name = modified_base_name + ".py"
executable_name = base_name + ".exe"
modified_executable_name = modified_base_name + ".exe"
spec_name = modified_base_name + ".spec"
workpath = "PyInstallerBuild"
distpath = "PyInstallerDist"

# Add extra package imports
print("To build PythonEvaluator, you will need the following packages installed: PyInstaller, socket, json, sys, time.")
print("Enter any extra packages you want to include in a comma-separated list:")
extra_packages = input().strip().split(",")

import_extra_packages = ""
for package in extra_packages:
  if not len(package) == 0 and not package.isspace(): # Don't import whitespace
    import_extra_packages += "import " + package + "\n"
    
    try:
        importlib.import_module(package)
    except ModuleNotFoundError:
        print(f"The package \"{package}\" is not installed.")
        input()
        exit()

with open(script_name, "r") as file:
    with open(modified_script_name, "w") as modified_file:
        modified_file.write(import_extra_packages + file.read())

# Create executable
PyInstaller.__main__.run([
    modified_script_name,
    "--noconfirm",
    "--onefile",
    "--console",
    "--workpath=" + workpath,
    "--distpath=" + distpath,
])

# Clean up
delete_folder(workpath)
shutil.move(distpath + "/" + modified_executable_name, modified_executable_name)
delete_folder(distpath)
delete_file(spec_name)
delete_file(modified_script_name)
if os.path.exists(executable_name):
    delete_file(executable_name)
os.rename(modified_executable_name, executable_name)

# Finished
print("Finished.")
input()
