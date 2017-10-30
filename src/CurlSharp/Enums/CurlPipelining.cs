// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
// 
// Copyright (c) 2017, Dr. Masroor Ehsan. All rights reserved.
// 
// $Id:$
// 
// Last modified: 25.01.2017 1:23 AM
// 
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

namespace CurlSharp
{
    /* bitmask bits for CURLMOPT_PIPELINING */

    public enum CurlPipelining : long
    {
        Nothing = 0,
        Http1 = 1,
        Multiplex = 2
    }
}