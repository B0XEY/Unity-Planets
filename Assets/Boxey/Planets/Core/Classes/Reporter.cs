using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Boxey.Planets.Core.Classes {
    public class Reporter {
        private List<string> _planetReports;

        public Reporter(int seed, int divisions, float chunkSize, float voxelSize, float planetRadius, string scaleLable) {
            var worldVoxelsCount = Mathf.Pow(Mathf.Pow(2, divisions - 1), 3) * Mathf.Pow(chunkSize / voxelSize, 3);
            AddReport("-----------------------------------------------");
            AddReport($"Seed for this generation: {seed}");
            AddReport($"Octree has a depth for this generation: {divisions}");
            AddReport($"Planet has a radius for this generation: {planetRadius:N0}m");
            AddReport(" ");
            AddReport($"The voxel scale for this generation: {scaleLable} ({voxelSize} meter)");
            AddReport($"The total number of voxels for this generation: {worldVoxelsCount:N0}");
            AddReport("-----------------------------------------------");
            AddReport(" ");
        }

        public void OnDebugLog(string logString, string stackTrace, LogType type) {
            if (!Application.isPlaying) {
                return;
            }
            var report = $"[{type}] {logString}";
            if (type == LogType.Exception) {
                report = stackTrace;
            }
            AddReport($"{report}");
        }
        public void AddReport(string report) {
            _planetReports ??= new List<string>();
            _planetReports.Add(report);
        }
        public void SaveToFile(string fileName) {
            var reportsFolder = Path.Combine(Application.persistentDataPath, "Planet Reports");
            if (!Application.isEditor) {
                reportsFolder = Path.Combine(Application.persistentDataPath, "Planet Reports", "Standalone");
            }
            if (!Directory.Exists(reportsFolder)) {
                Directory.CreateDirectory(reportsFolder);
            }
            var fullPath  = Path.Combine(reportsFolder,  $"{fileName}.rpt");
            if (!Application.isEditor) {
                fullPath  = Path.Combine(reportsFolder,  $"{fileName}({DateTime.Now:dd-hh-mm}).rpt");
            }
            if (!File.Exists(fullPath)) {
                File.Create(fullPath).Close();
            }
            File.WriteAllLines(fullPath, _planetReports.ToArray());
        }
    }
}