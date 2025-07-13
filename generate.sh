#!/bin/bash

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
mkdir -p gen
schemas=$(find Protocol/schemas -name '*.proto')
bin/protoc -I=Protocol/schemas --csharp_out=gen $schemas
