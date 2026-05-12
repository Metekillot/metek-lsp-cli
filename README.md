[![Free as in Freedom](https://www.gnu.org/graphics/lgplv3-with-text-154x68.png)](https://www.gnu.org/licenses/lgpl-3.0.md)
# metek-lsp-cli

## It ain't much, but it's performant

A good-enough-practices implementation of a CLI interface for the LSP ( Language Server Protocol ). It's a personal tool for me, primarily, for cases where I'd like to automate the tools that LSP provides for doing mass refactors or just general code auditing.

Has special support for [SpacemanDMM's object trees for DreamMaker](https://github.com/SpaceManiac/SpacemanDMM); otherwise, it is slowly gaining robust support for LSP tooling that would be most convenient from the command line.

# csharp-language-server-protocol

As stated within [the LICENSE.csharp-language-server-protocol](./LICENSE.csharp-language-server-protocol) file in this directory and additionally within their own licensing. [the csharp-language-server-protocol fork used as a library and kept as a module](lib/csharp-language-server-protocol/) remains covered under the MIT license, with All Rights Reserved by the .NET Foundation and Contributors

## Currently implemented:

### Hover

### Definition

### References

### Workspace symbols

### Semantic token decoding

### Document symbols

### Implementations

### Categorization and storage of notifications from the server

### Function signature help

### And more
See DriveRequestsConfig.json for built-in supported request handles adapted from the Omnisharp LSP protocol. AnnotationRange and ObjectTree are exclusive to the [metek-lsp-cli-submodule branch of this SpacemanDMM fork](https://github.com/Metekillot/SpacemanDMM)