// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
// 
// Copyright (c) 2017, Dr. Masroor Ehsan. All rights reserved.
// 
// $Id:$
// 
// Last modified: 25.01.2017 1:29 AM
// 
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *

namespace CurlSharp
{
    public enum CurlMultiOption
    {
        /* This is the socket callback function pointer */
        SocketFunction = CurlOptType.FunctionPoint + 1,
        /* This is the argument passed to the socket callback */
        SocketData = CurlOptType.ObjectPoint + 2,
        /* set to 1 to enable pipelining for this multi handle */
        Pipelining = CurlOptType.Long + 3,
        /* This is the timer callback function pointer */
        TimerFunction = CurlOptType.FunctionPoint + 4,
        /* This is the argument passed to the timer callback */
        TimerDate = CurlOptType.ObjectPoint + 5,
        /* maximum number of entries in the connection cache */
        MaxConnects = CurlOptType.Long + 6,
        /* maximum number of (pipelining) connections to one host */
        MaxHostConnections = CurlOptType.Long + 7,
        /* maximum number of requests in a pipeline */
        MaxPipelineLength = CurlOptType.Long + 8,
        /* a connection with a content-length longer than this will not be considered for pipelining */
        ContentLengthPenaltySize = CurlOptType.Offset + 9,
        /* a connection with a chunk length longer than this will not be considered for pipelining */
        ChunkLengthPenaltySize = CurlOptType.Offset + 10,
        /* a list of site names(+port) that are blacklisted from pipelining */
        PipeliningSiteBlackList = CurlOptType.ObjectPoint + 11,
        /* a list of server types that are blacklisted from pipelining */
        PipeliningServerBlackList = CurlOptType.ObjectPoint + 12,
        /* maximum number of open connections in total */
        MaxTotalConnections = CurlOptType.Long + 13,
        /* This is the server push callback function pointer */
        PushFunction = CurlOptType.FunctionPoint + 14,
        /* This is the argument passed to the server push callback */
        PushData = CurlOptType.ObjectPoint + 15
    }
}