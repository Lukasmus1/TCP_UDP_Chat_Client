ARGS :=

all: publish

build:
	dotnet build

publish:
	dotnet publish -r linux-x64 -o ./ -p:PublishSingleFile=true --self-contained true

run:
	dotnet run --project IPK_Project.csproj -- $(ARGS)

clean:
	dotnet clean
	rm -rf ./bin ./obj
	rm -f ./ipk24chat-client
	rm -f ./ipk24chat-client.pdb
    
.PHONY: all build run clean