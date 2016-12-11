// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php
namespace Brainsharp

open FParsec

type BFError = 
    | FileNotExist of string
    | InvalidArguments
    | ParseError of string * ParserError
    | UnexpectedEndOfInput
