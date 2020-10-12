cd src\CustomTasks
dotnet publish --framework netcoreapp2.1
cd ..\..

cd src\TestProject
dotnet exec "C:/Program Files/dotnet/sdk/3.1.401/MSBuild.dll" /t:Clean;Restore;Pack /p:CustomTasksEnabled=True /bl
cd ..\..