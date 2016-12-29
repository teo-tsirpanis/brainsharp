// Copyright (c) 2016 Theodore Tsirpanis
// 
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

namespace Brainsharp

open BFCode
open RLE

module Optimizer =
    let optimize program = overKill id program
