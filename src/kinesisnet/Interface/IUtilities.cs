﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Kinesis.Model;
using Serilog;

namespace KinesisNet.Interface
{
    public interface IUtilities
    {
        string StreamName { get; }
        string WorkerId { get; }
        SplitShardResponse SplitShard(Shard shard);
        MergeShardsResponse MergeShards(string leftShard, string rightShard);
        DescribeStreamResponse GetStreamResponse();
        Task<DescribeStreamResponse> GetStreamResponseAsync();
        IList<Shard> GetActiveShards();
        IList<Shard> GetDisabledShards();
        IList<Shard> GetShards();
        Task<IList<Shard>> GetDisabledShardsAsync();
        Task<IList<Shard>> GetActiveShardsAsync();
        Task<IList<Shard>> GetShardsAsync();

        int DynamoReadCapacityUnits { get; }
        int DynamoWriteCapacityUnits { get; }

        IUtilities SetDynamoReadCapacityUnits(int readCapacityUnits);
        IUtilities SetDynamoWriteCapacityUnits(int writeCapacityUnits);

        IUtilities SetLogConfiguration(LoggerConfiguration configuration);
        ILogger Log { get; }
    }
}
