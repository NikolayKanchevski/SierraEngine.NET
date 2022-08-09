from fileinput import filename
import os

linesOfCode = 0
DIRECTORIES = ["Core", "Engine"]

for directory in DIRECTORIES:
    for root, dirs, files in os.walk(directory + "/"):
        for file in files:
            if file.endswith(".cs"):
                fileName = str(os.path.join(root, file))
                with open(fileName, 'r') as fp:
                    for count, line in enumerate(fp):
                        pass
                linesOfCode += count + 1
                
readMeData = ""
with open('README.md', 'r') as file:
    data = file.read()
    index = data.index("<p align=\"center\" id=\"LinesCounter\">")
    data = data[0:index]

    newLine = f"<p align=\"center\" id=\"LinesCounter\">Total lines of code: { linesOfCode }</p>\n\n" + ("-" * 171)
    readMeData = data + newLine

with open('README.md', 'w') as file:
    file.write(readMeData)