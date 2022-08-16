using Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VCILib.JobManagment;

namespace VCILib.JobManagment
{
    public static class DiagTaskReader
    {
        public static DiagTask Read(string filename)
        {
            if (!File.Exists(filename))
                return null;
            var timer = new UDSTimingParams();
            var sequence = new JobSequence();
            if (filename.EndsWith(".csv"))
            {
                string[] lines = null;
                try
                {
                      lines = File.ReadAllLines(filename, Encoding.UTF8);
                }
                catch (Exception e)
                {
                    e.LogToFile();
                    return null;
                }

                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (line .StartsWith( "UDSTimingParams") && i + 1 < lines.Length)
                    {
                        for (var l = lines[++i]; l.Length > 1; l = lines[++i])
                        {
                            var content = l.Split(',',StringSplitOptions.RemoveEmptyEntries);
                            if (content.Length >= 2)
                                try
                                {
                                    var info = typeof(UDSTimingParams).GetField(content[0], System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                                    if (info != null) info.SetValue(timer, Convert.ChangeType(content[1], info.FieldType));
                                }
                                catch (Exception) { }
                            else break;
                            if (i == lines.Length-1) break;
                        }
                    }
                    if (line.StartsWith("JobSequence") && i + 3 < lines.Length)
                    {
                        var fieldsNames = lines[i += 2].Split(',',StringSplitOptions.RemoveEmptyEntries);
                        var fields = fieldsNames.Select(f => typeof(JobSequenceItem).GetProperty(f, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)).ToArray();
                        if (fields == null || fields.Length == 0)
                            return null;
                        for (var l = lines[++i]; l.Length > 1; l = lines[++i])
                        {
                            var job = new JobSequenceItem()  ;
                            var content = l.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            if (content.Length >= 3)
                            {
                                for (int col = 0; col <Math.Min(content.Length, fields.Length); col++)
                                {
                                    fields[col]?.SetValue(job, Convert.ChangeType(content[col], fields[col].PropertyType));
                                }
                            }
                            else break;
                            sequence.Items.Add(job);
                            if (i == lines.Length - 1) break;
                        }
                    }

                }
            }
            sequence.TimingParameters = timer;
            return new DiagTask() { Jobs = sequence, TimingParams = timer };
        }
    }
}
