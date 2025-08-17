#!/bin/bash

set -e

download=$1

# Download protoc
if [ "$download" == "1" ]; then
    mkdir -p bin
    cd bin
    
    wget https://github.com/protocolbuffers/protobuf/releases/download/v31.1/protoc-31.1-linux-x86_64.zip
    unzip protoc-31.1-linux-x86_64.zip -d protoc_base
    mv protoc_base/bin/protoc .
    mv protoc_base/include .
    
    cd -
fi

# Generate protocol codes
schema_dir="Protocol/schema"
gen_dir="gen"

mkdir -p $gen_dir
schemas=$(find $schema_dir -name '*.proto')
bin/protoc -I=$schema_dir --csharp_out=$gen_dir $schemas

dotnet run --project Generator -- protocol --schema_dir Protocol/schema --gen_dir gen
