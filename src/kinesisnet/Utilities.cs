﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Amazon.Kinesis;
using Amazon.Kinesis.Model;
using KinesisNet.Interface;
using Serilog;

namespace KinesisNet
{
    internal class Utilities : IUtilities
    {
        private readonly AmazonKinesisClient _client;
        private readonly string _streamName;

        private string _workerId;
        private ILogger _logger;

        private int _readCapacityUnits;
        private int _writeCapacityUnits;

        private static ConcurrentDictionary<string, string> _workingConsumer; 

        public Utilities(AmazonKinesisClient client, string streamName, string workerId)
        {
            _client = client;
            _streamName = streamName;

            _readCapacityUnits = 1;
            _writeCapacityUnits = 1;

            if (_workingConsumer == null)
            {
                _workingConsumer = new ConcurrentDictionary<string, string>();
            }

            SetWorkerId(workerId, _streamName);

            _logger = new LoggerConfiguration()
                .WriteTo
                .ColoredConsole()
                .CreateLogger();

            Serilog.Log.Logger = _logger;
        }

        public string WorkerId
        {
            get { return _workerId; }
        }

        public string StreamName
        {
            get { return _streamName; }
        }

        public int DynamoReadCapacityUnits
        {
            get { return _readCapacityUnits; }
        }

        public int DynamoWriteCapacityUnits
        {
            get { return _writeCapacityUnits; }
        }

        public IUtilities SetLogConfiguration(LoggerConfiguration configuration)
        {
            _logger = configuration.CreateLogger();

            Serilog.Log.Logger = _logger;

            return this;
        }

        public ILogger Log
        {
            get { return _logger; }
        }

        public SplitShardResponse SplitShard(Shard shard)
        {
            var startingHashKey = BigInteger.Parse(shard.HashKeyRange.StartingHashKey);
            var endingHashKey = BigInteger.Parse(shard.HashKeyRange.EndingHashKey);
            var newStartingHashKey = BigInteger.Divide(BigInteger.Add(startingHashKey, endingHashKey), new BigInteger(2));

            var splitShardRequest = new SplitShardRequest { StreamName = _streamName, ShardToSplit = shard.ShardId, NewStartingHashKey = newStartingHashKey.ToString() };

            var response = _client.SplitShard(splitShardRequest);

            return response;
        }

        public MergeShardsResponse MergeShards(string leftShard, string rightShard)
        {
            var mergeShardRequest = new MergeShardsRequest
            {
                ShardToMerge = leftShard,
                AdjacentShardToMerge = rightShard,
                StreamName = _streamName
            };

            var response = _client.MergeShards(mergeShardRequest);

            return response;
        }

        public DescribeStreamResponse GetStreamResponse()
        {
            var request = new DescribeStreamRequest() { StreamName = _streamName };

            return _client.DescribeStream(request);
        }

        public async Task<DescribeStreamResponse> GetStreamResponseAsync()
        {
            var request = new DescribeStreamRequest() { StreamName = _streamName };

            return await _client.DescribeStreamAsync(request);
        }

        public IList<Shard> GetActiveShards()
        {
            return GetShards().Where(m => m.SequenceNumberRange.EndingSequenceNumber == null).ToList();
        }

        public IList<Shard> GetDisabledShards()
        {
            return GetShards().Where(m => m.SequenceNumberRange.EndingSequenceNumber != null).ToList();
        }

        public IList<Shard> GetShards()
        {
            var stream = GetStreamResponse();

            return stream.StreamDescription.Shards;
        }

        public async Task<IList<Shard>> GetDisabledShardsAsync()
        {
            var shards = await GetShardsAsync();

            return shards.Where(m => m.SequenceNumberRange.EndingSequenceNumber != null).ToList();
        }

        public async Task<IList<Shard>> GetActiveShardsAsync()
        {
            var shards = await GetShardsAsync();

            return shards.Where(m => m.SequenceNumberRange.EndingSequenceNumber == null).ToList();
        }

        public async Task<IList<Shard>> GetShardsAsync()
        {
            var stream = await GetStreamResponseAsync();

            return stream.StreamDescription.Shards;
        }

        private IUtilities SetWorkerId(string workerId, string streamName)
        {
            if (string.IsNullOrEmpty(workerId) || string.IsNullOrEmpty(streamName))
            {
                throw  new ArgumentException("The WorkerId or the stream name cannot be null");
            }

            string existingStreamName;
            if (_workingConsumer.TryGetValue(workerId, out existingStreamName))
            {
                if (string.Equals(existingStreamName, streamName))
                {
                    throw new ArgumentException("You cannot run more than one instance of the consumer for the same stream name");
                }
            }

            if (!_workingConsumer.TryAdd(workerId, streamName))
            {
                throw new ArgumentException("Could not add workerId");
            }

            _workerId = workerId;

            return this;
        }

        public IUtilities SetDynamoReadCapacityUnits(int readCapacityUnits)
        {
            _readCapacityUnits = readCapacityUnits;

            return this;
        }

        public IUtilities SetDynamoWriteCapacityUnits(int writeCapacityUnits)
        {
            _writeCapacityUnits = writeCapacityUnits;

            return this;
        }
    }
}
