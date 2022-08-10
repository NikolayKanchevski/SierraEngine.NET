import datetime
from fileinput import filename
import os

linesOfCode = 0
DIRECTORIES = ["Core", "Engine"]

now = datetime.datetime.now()
lastUpdated = f"{now.day:02}/{now.month:02}/{now.year:02}"

for directory in DIRECTORIES:
    for root, dirs, files in os.walk(directory + "/"):
        for file in files:
            if file.endswith(".cs"):
                fileName = str(os.path.join(root, file))
                with open(fileName, 'r') as fp:
                    for count, line in enumerate(fp):
                        pass
                linesOfCode += count + 1
                
newReadMeData = ""
with open('README.md', 'r') as file:
    readMeData = file.read()
    index = readMeData.index("<p align=\"center\" id=\"LinesCounter\">")
    readMeData = readMeData[0:index]

    linesOfCodeLine = f"<p align=\"center\" id=\"LinesCounter\">Total lines of code: { linesOfCode }</p>\n"
    lastUpdatedLine = f"<p align=\"center\" id=\"LastUpdated\">Last updated: { lastUpdated }</p>\n"
    
    newReadMeData = readMeData + linesOfCodeLine + lastUpdatedLine + "\n" + ("-" * 171)

with open('README.md', 'w') as file:
    file.write(newReadMeData)