# Binlog fixtures

Generate a small temporary project in a test, then run dotnet build -bl:build.binlog.
Never commit the binary. Binlog interpretation is delegated to an optional structured
MSBuild tool; AgentTool does not pretend to parse binlogs as text.
