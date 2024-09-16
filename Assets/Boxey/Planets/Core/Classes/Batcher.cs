using System.Collections.Generic;
using Boxey.Planets.Core.Static;
using UnityEngine;

namespace Boxey.Planets.Core.Classes {
    public abstract class Batcher {
        public static List<List<UpdateCalls.NodeInfo>> CreateTerraformBatches(List<Node> hashSet, int batchSize, int currentIndex) {
            var batches = new List<List<UpdateCalls.NodeInfo>>();
            var currentBatch = new HashSet<UpdateCalls.NodeInfo>();
        
            foreach (var node in hashSet) {
                if (node.GetTerraformIndex() == currentIndex) {
                    continue;
                }
                var nodeScale = node.NodeScale();
                var info = new UpdateCalls.NodeInfo(nodeScale, node.GetTerraformIndex(), node.NodeLocalPosition(), Vector3.one * (nodeScale / 2f), node.GetTerraformMap());
                currentBatch.Add(info);

                if (currentBatch.Count >= batchSize) {
                    batches.Add(new List<UpdateCalls.NodeInfo>(currentBatch)); // Add a new batch with the current nodes
                    currentBatch.Clear(); // Clear for the next batch
                }
            }

            // Add the last batch if it has leftover elements
            if (currentBatch.Count > 0) {
                batches.Add(new List<UpdateCalls.NodeInfo>(currentBatch));
            }

            return batches;
        }
    }
}