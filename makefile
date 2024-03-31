DIR := ./IPK_Project
ARGS :=

all: build

build:
	@dotnet publish $(DIR) -o ./publish -p:PublishSingleFile=true

run:
	@dotnet run --project $(DIR)/IPK_Project.csproj -- $(ARGS)	

clean:
	@dotnet clean $(DIR)
	@rm -rf ./publish
    
.PHONY: all build run clean