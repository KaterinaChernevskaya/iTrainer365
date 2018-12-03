@echo off
setlocal

date /t >> ..\compilation_resources.log
time /t >> ..\compilation_resources.log

for %%f in (.\messages\Resources\Localization\*.*.txt) do (  
  .\resgen.exe %%f %%~pf%%~nf.resources >> ..\compilation_resources.log
)

echo ---------------------- >> ..\compilation_resources.log
echo Compilation resources done