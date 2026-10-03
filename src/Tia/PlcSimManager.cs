using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Siemens.Simatic.Simulation.Runtime;

namespace TiaOpennessMcp.Tia
{
    public class PlcSimManager
    {
        private static readonly Lazy<PlcSimManager> _lazy = new Lazy<PlcSimManager>(() => new PlcSimManager());
        public static PlcSimManager Instance => _lazy.Value;

        private IInstance? _instance;
        private string? _connectedName;
        private readonly Dictionary<string, EDataType> _tagTypeCache = new Dictionary<string, EDataType>(StringComparer.OrdinalIgnoreCase);

        private PlcSimManager() { }

        public bool IsConnected => _instance != null;
        public string? ConnectedInstanceName => _connectedName;

        public List<PlcSimInstanceSummary> ListInstances()
        {
            var result = new List<PlcSimInstanceSummary>();
            try
            {
                var infos = SimulationRuntimeManager.RegisteredInstanceInfo;
                if (infos != null)
                {
                    foreach (var info in infos)
                    {
                        var summary = new PlcSimInstanceSummary
                        {
                            ID = info.ID,
                            Name = info.Name
                        };
                        try
                        {
                            var inst = SimulationRuntimeManager.CreateInterface(info.ID);
                            if (inst != null)
                            {
                                summary.OperatingState = inst.OperatingState.ToString();
                                summary.CPUType = inst.CPUType.ToString();
                                summary.IP = inst.ControllerIP != null ? string.Join(", ", inst.ControllerIP) : "";
                            }
                        }
                        catch
                        {
                            // If instance is not reachable
                        }
                        result.Add(summary);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PlcSimManager] ListInstances error: {ex.Message}");
            }
            return result;
        }

        public PlcSimConnectResult Connect(string instanceName = "PLC_1")
        {
            try
            {
                _instance = SimulationRuntimeManager.CreateInterface(instanceName);
                _connectedName = instanceName;

                // Cache tag metadata
                RefreshTagCache();

                return new PlcSimConnectResult
                {
                    Success = true,
                    InstanceName = instanceName,
                    ID = _instance.ID,
                    OperatingState = _instance.OperatingState.ToString(),
                    OperatingMode = _instance.OperatingMode.ToString(),
                    CPUType = _instance.CPUType.ToString(),
                    IP = _instance.ControllerIP != null ? string.Join(", ", _instance.ControllerIP) : "",
                    ScaleFactor = _instance.ScaleFactor,
                    TagCount = _tagTypeCache.Count,
                    Message = $"Successfully connected to S7-PLCSIM Advanced instance '{instanceName}'."
                };
            }
            catch (Exception ex)
            {
                return new PlcSimConnectResult
                {
                    Success = false,
                    InstanceName = instanceName,
                    Message = $"Failed to connect to PLCSIM Advanced instance '{instanceName}': {ex.Message}"
                };
            }
        }

        public void Disconnect()
        {
            _instance = null;
            _connectedName = null;
            _tagTypeCache.Clear();
        }

        private void EnsureConnected()
        {
            if (_instance == null)
            {
                var conn = Connect(_connectedName ?? "PLC_1");
                if (!conn.Success || _instance == null)
                {
                    throw new InvalidOperationException("Not connected to S7-PLCSIM Advanced. Call 'plcsim_connect' first.");
                }
            }
        }

        public void RefreshTagCache()
        {
            if (_instance == null) return;
            try
            {
                _instance.UpdateTagList();
                _tagTypeCache.Clear();
                if (_instance.TagInfos != null)
                {
                    foreach (var tag in _instance.TagInfos)
                    {
                        if (!string.IsNullOrEmpty(tag.Name))
                        {
                            _tagTypeCache[tag.Name] = tag.DataType;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[PlcSimManager] RefreshTagCache error: {ex.Message}");
            }
        }

        public PlcSimStatusResult GetStatus()
        {
            EnsureConnected();
            return new PlcSimStatusResult
            {
                Name = _instance!.Name,
                ID = _instance.ID,
                OperatingState = _instance.OperatingState.ToString(),
                OperatingMode = _instance.OperatingMode.ToString(),
                CPUType = _instance.CPUType.ToString(),
                IP = _instance.ControllerIP != null ? string.Join(", ", _instance.ControllerIP) : "",
                ScaleFactor = _instance.ScaleFactor,
                TagCount = _tagTypeCache.Count
            };
        }

        public bool SetOperatingState(string state)
        {
            EnsureConnected();
            if (string.Equals(state, "Run", StringComparison.OrdinalIgnoreCase))
            {
                _instance!.Run();
                return true;
            }
            if (string.Equals(state, "Stop", StringComparison.OrdinalIgnoreCase))
            {
                _instance!.Stop();
                return true;
            }
            if (string.Equals(state, "MemoryReset", StringComparison.OrdinalIgnoreCase))
            {
                _instance!.MemoryReset();
                return true;
            }
            throw new ArgumentException($"Invalid OperatingState '{state}'. Valid: Run, Stop, MemoryReset.");
        }

        public double SetScaleFactor(double scaleFactor)
        {
            EnsureConnected();
            _instance!.ScaleFactor = scaleFactor;
            return _instance.ScaleFactor;
        }

        public TagValueDto ReadTag(string tagName)
        {
            EnsureConnected();

            _tagTypeCache.TryGetValue(tagName, out var dt);
            object val = ReadTypedValue(tagName, dt);

            return new TagValueDto
            {
                TagName = tagName,
                Value = val,
                DataType = dt.ToString()
            };
        }

        public List<TagValueDto> ReadTags(IEnumerable<string> tagNames)
        {
            EnsureConnected();
            var results = new List<TagValueDto>();
            foreach (var name in tagNames)
            {
                try
                {
                    results.Add(ReadTag(name));
                }
                catch (Exception ex)
                {
                    results.Add(new TagValueDto
                    {
                        TagName = name,
                        Value = null,
                        DataType = "Error: " + ex.Message
                    });
                }
            }
            return results;
        }

        public bool WriteTag(string tagName, object value)
        {
            EnsureConnected();
            _tagTypeCache.TryGetValue(tagName, out var dt);
            WriteTypedValue(tagName, value, dt);
            return true;
        }

        public int WriteTags(Dictionary<string, object> tags)
        {
            EnsureConnected();
            int count = 0;
            foreach (var kvp in tags)
            {
                if (WriteTag(kvp.Key, kvp.Value)) count++;
            }
            return count;
        }

        private static object ExtractValue(SDataValue sv)
        {
            switch (sv.Type)
            {
                case EPrimitiveDataType.Bool: return sv.Bool;
                case EPrimitiveDataType.Int8: return sv.Int8;
                case EPrimitiveDataType.UInt8: return sv.UInt8;
                case EPrimitiveDataType.Int16: return sv.Int16;
                case EPrimitiveDataType.UInt16: return sv.UInt16;
                case EPrimitiveDataType.Int32: return sv.Int32;
                case EPrimitiveDataType.UInt32: return sv.UInt32;
                case EPrimitiveDataType.Int64: return sv.Int64;
                case EPrimitiveDataType.UInt64: return sv.UInt64;
                case EPrimitiveDataType.Float: return sv.Float;
                case EPrimitiveDataType.Double: return sv.Double;
                case EPrimitiveDataType.Char: return sv.Char;
                case EPrimitiveDataType.WChar: return sv.WChar;
                default: return sv.ToString();
            }
        }

        private object ReadTypedValue(string tagName, EDataType dt)
        {
            try
            {
                switch (dt)
                {
                    case EDataType.Bool:
                        return _instance!.ReadBool(tagName);
                    case EDataType.Byte:
                        return _instance!.ReadUInt8(tagName);
                    case EDataType.Word:
                        return _instance!.ReadUInt16(tagName);
                    case EDataType.DWord:
                        return _instance!.ReadUInt32(tagName);
                    case EDataType.Int:
                        return _instance!.ReadInt16(tagName);
                    case EDataType.DInt:
                    case EDataType.Time:
                        return _instance!.ReadInt32(tagName);
                    case EDataType.LInt:
                        return _instance!.ReadInt64(tagName);
                    case EDataType.Real:
                        return _instance!.ReadFloat(tagName);
                    case EDataType.LReal:
                        return _instance!.ReadDouble(tagName);
                    case EDataType.String:
                        return _instance!.ReadString(tagName);
                    case EDataType.WString:
                        return _instance!.ReadWString(tagName);
                    default:
                        // Try fallback methods
                        try { return _instance!.ReadFloat(tagName); } catch { }
                        try { return _instance!.ReadInt32(tagName); } catch { }
                        try { return _instance!.ReadBool(tagName); } catch { }
                        try { return _instance!.ReadString(tagName); } catch { }
                        return ExtractValue(_instance!.Read(tagName));
                }
            }
            catch
            {
                // Fallback direct read
                return ExtractValue(_instance!.Read(tagName));
            }
        }

        private void WriteTypedValue(string tagName, object value, EDataType dt)
        {
            if (value is JsonElement je)
            {
                value = ConvertJsonElement(je, dt);
            }

            switch (dt)
            {
                case EDataType.Bool:
                    _instance!.WriteBool(tagName, Convert.ToBoolean(value));
                    break;
                case EDataType.Byte:
                    _instance!.WriteUInt8(tagName, Convert.ToByte(value));
                    break;
                case EDataType.Word:
                    _instance!.WriteUInt16(tagName, Convert.ToUInt16(value));
                    break;
                case EDataType.DWord:
                    _instance!.WriteUInt32(tagName, Convert.ToUInt32(value));
                    break;
                case EDataType.Int:
                    _instance!.WriteInt16(tagName, Convert.ToInt16(value));
                    break;
                case EDataType.DInt:
                case EDataType.Time:
                    _instance!.WriteInt32(tagName, Convert.ToInt32(value));
                    break;
                case EDataType.LInt:
                    _instance!.WriteInt64(tagName, Convert.ToInt64(value));
                    break;
                case EDataType.Real:
                    _instance!.WriteFloat(tagName, Convert.ToSingle(value));
                    break;
                case EDataType.LReal:
                    _instance!.WriteDouble(tagName, Convert.ToDouble(value));
                    break;
                case EDataType.String:
                    _instance!.WriteString(tagName, Convert.ToString(value) ?? "");
                    break;
                case EDataType.WString:
                    _instance!.WriteWString(tagName, Convert.ToString(value) ?? "");
                    break;
                default:
                    if (value is bool b) _instance!.WriteBool(tagName, b);
                    else if (value is float f) _instance!.WriteFloat(tagName, f);
                    else if (value is double d) _instance!.WriteFloat(tagName, (float)d);
                    else if (value is int i) _instance!.WriteInt32(tagName, i);
                    else if (value is string s) _instance!.WriteString(tagName, s);
                    else _instance!.WriteString(tagName, value.ToString() ?? "");
                    break;
            }
        }

        private static object ConvertJsonElement(JsonElement je, EDataType dt)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.True: return true;
                case JsonValueKind.False: return false;
                case JsonValueKind.Number:
                    if (dt == EDataType.Real || dt == EDataType.LReal)
                    {
                        if (je.TryGetSingle(out var s)) return s;
                        if (je.TryGetDouble(out var d)) return d;
                    }
                    if (je.TryGetInt32(out var i)) return i;
                    if (je.TryGetInt64(out var l)) return l;
                    if (je.TryGetDouble(out var num)) return num;
                    return 0;
                case JsonValueKind.String:
                    return je.GetString() ?? "";
                default:
                    return je.ToString();
            }
        }

        public SequenceRunReport RunSequence(SequenceExecutionInput input, double scaleFactor = 1.0, int timeoutSeconds = 60)
        {
            EnsureConnected();

            double prevScale = _instance!.ScaleFactor;
            if (scaleFactor > 0 && Math.Abs(scaleFactor - prevScale) > 0.01)
            {
                _instance.ScaleFactor = scaleFactor;
            }

            var report = new SequenceRunReport
            {
                SequenceName = input.SequenceName,
                ScaleFactor = _instance.ScaleFactor,
                StartTime = DateTime.UtcNow
            };

            var sw = Stopwatch.StartNew();

            try
            {
                // 1. Initial State Preconditioning
                if (input.InitialTags != null && input.InitialTags.Count > 0)
                {
                    WriteTags(input.InitialTags);
                }

                // 2. Pulse Reset if configured
                if (!string.IsNullOrEmpty(input.ResetCommandTag))
                {
                    WriteTag(input.ResetCommandTag!, true);
                    Thread.Sleep(50);
                    WriteTag(input.ResetCommandTag!, false);
                    Thread.Sleep(50);
                }

                // 3. Write Recipe / Parameter Tags
                if (input.ParameterTags != null && input.ParameterTags.Count > 0)
                {
                    WriteTags(input.ParameterTags);
                }

                // 4. Pulse Start Command
                if (!string.IsNullOrEmpty(input.StartCommandTag))
                {
                    WriteTag(input.StartCommandTag, true);
                    Thread.Sleep(100);
                    WriteTag(input.StartCommandTag, false);
                }

                // 5. Monitoring Loop
                int currentStep = -1;
                string currentStepName = "";
                DateTime stepStartTime = DateTime.UtcNow;
                bool completed = false;
                bool faulted = false;

                while (sw.Elapsed.TotalSeconds < timeoutSeconds)
                {
                    int step = 0;
                    if (!string.IsNullOrEmpty(input.StepNumberTag))
                    {
                        var stepVal = ReadTag(input.StepNumberTag).Value;
                        if (stepVal != null && int.TryParse(stepVal.ToString(), out int sVal))
                        {
                            step = sVal;
                        }
                    }

                    string stepName = "";
                    if (!string.IsNullOrEmpty(input.StepNameTag))
                    {
                        stepName = ReadTag(input.StepNameTag!).Value?.ToString() ?? "";
                    }

                    bool done = false;
                    if (!string.IsNullOrEmpty(input.DoneTag))
                    {
                        var doneVal = ReadTag(input.DoneTag).Value;
                        if (doneVal is bool bDone) done = bDone;
                        else if (doneVal != null && bool.TryParse(doneVal.ToString(), out bool pbDone)) done = pbDone;
                    }

                    bool fault = false;
                    if (!string.IsNullOrEmpty(input.FaultTag))
                    {
                        var fVal = ReadTag(input.FaultTag!).Value;
                        if (fVal is bool bFault) fault = bFault;
                        else if (fVal != null && bool.TryParse(fVal.ToString(), out bool pbFault)) fault = pbFault;
                    }

                    // Step transition detection
                    if (step != currentStep)
                    {
                        if (currentStep >= 0)
                        {
                            report.StepTransitions.Add(new StepTransitionInfo
                            {
                                StepNumber = currentStep,
                                StepName = currentStepName,
                                DurationSeconds = Math.Round((DateTime.UtcNow - stepStartTime).TotalSeconds, 2)
                            });
                        }
                        currentStep = step;
                        currentStepName = stepName;
                        stepStartTime = DateTime.UtcNow;
                    }

                    // Sample monitored telemetry
                    var sample = new Dictionary<string, object>
                    {
                        { "ElapsedSeconds", Math.Round(sw.Elapsed.TotalSeconds, 2) },
                        { "StepNumber", step },
                        { "StepName", stepName }
                    };

                    if (input.MonitoredTags != null)
                    {
                        foreach (var tag in input.MonitoredTags)
                        {
                            try
                            {
                                sample[tag] = ReadTag(tag).Value ?? "";
                            }
                            catch
                            {
                                sample[tag] = "";
                            }
                        }
                    }

                    report.Telemetry.Add(sample);

                    if (fault)
                    {
                        faulted = true;
                        report.ErrorMessage = $"Sequence faulted during Step {step} ('{stepName}').";
                        break;
                    }

                    if (done && step == input.IdleStep && sw.Elapsed.TotalSeconds > 1.0)
                    {
                        completed = true;
                        break;
                    }

                    Thread.Sleep(80);
                }

                if (currentStep >= 0)
                {
                    report.StepTransitions.Add(new StepTransitionInfo
                    {
                        StepNumber = currentStep,
                        StepName = currentStepName,
                        DurationSeconds = Math.Round((DateTime.UtcNow - stepStartTime).TotalSeconds, 2)
                    });
                }

                report.EndTime = DateTime.UtcNow;
                report.TotalDurationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
                report.Success = completed && !faulted;

                if (!completed && !faulted)
                {
                    report.ErrorMessage = $"Execution timed out after {timeoutSeconds}s at Step {currentStep}.";
                }
            }
            finally
            {
                if (scaleFactor > 0 && Math.Abs(scaleFactor - prevScale) > 0.01)
                {
                    try { _instance!.ScaleFactor = prevScale; } catch { }
                }
            }

            return report;
        }

        public MixingSequenceRunReport RunMixingSequence(MixingRecipeInput recipe, double scaleFactor = 1.0, int timeoutSeconds = 60)
        {
            EnsureConnected();

            double prevScale = _instance!.ScaleFactor;
            if (scaleFactor > 0 && Math.Abs(scaleFactor - prevScale) > 0.01)
            {
                _instance.ScaleFactor = scaleFactor;
            }

            var report = new MixingSequenceRunReport
            {
                Recipe = recipe,
                ScaleFactor = _instance.ScaleFactor,
                StartTime = DateTime.UtcNow
            };

            var sw = Stopwatch.StartNew();

            try
            {
                // 1. Initial State Preconditioning
                _instance.WriteBool("DB_MixingLine.Line.EmergencyStopOk", true);
                _instance.WriteBool("DB_MixingLine.Simulate", true);
                _instance.WriteBool("DB_MixingLine.Cmd_Reset", true);
                Thread.Sleep(50);
                _instance.WriteBool("DB_MixingLine.Cmd_Reset", false);
                Thread.Sleep(50);

                // 2. Set Recipe Parameters
                _instance.WriteWString("DB_MixingLine.BatchRecipe.RecipeID", recipe.RecipeID);
                _instance.WriteFloat("DB_MixingLine.BatchRecipe.Water", recipe.WaterLiters);
                _instance.WriteInt16("DB_MixingLine.BatchRecipe.Additive.Type", (short)recipe.AdditiveType);
                _instance.WriteFloat("DB_MixingLine.BatchRecipe.Additive.Amount", recipe.AdditiveAmount);
                _instance.WriteFloat("DB_MixingLine.BatchRecipe.Sugar", recipe.SugarKg);
                int mixTimeMs = (int)(recipe.MixTimeSeconds * 1000.0);
                _instance.WriteInt32("DB_MixingLine.BatchRecipe.MixTime", mixTimeMs);
                _instance.WriteFloat("DB_MixingLine.BatchRecipe.MixSpeed", recipe.MixSpeedRpm);

                // 3. Pulse Start Batch
                _instance.WriteBool("DB_MixingLine.Cmd_StartBatch", true);
                Thread.Sleep(100);
                _instance.WriteBool("DB_MixingLine.Cmd_StartBatch", false);

                // 4. Monitoring Loop
                int currentStep = -1;
                string currentStepName = "";
                DateTime stepStartTime = DateTime.UtcNow;
                bool completed = false;
                bool faulted = false;

                while (sw.Elapsed.TotalSeconds < timeoutSeconds)
                {
                    int step = _instance.ReadInt16("DB_MixingLine.Status_StepNumber");
                    string stepName = _instance.ReadString("DB_MixingLine.Status_StepName");
                    bool busy = _instance.ReadBool("DB_MixingLine.Status_Busy");
                    bool batchDone = _instance.ReadBool("DB_MixingLine.Status_BatchDone");
                    bool fault = _instance.ReadBool("DB_MixingLine.Status_Fault");

                    float tankLevel = _instance.ReadFloat("DB_MixingLine.Line.MixingTank.CurrentLevel");
                    float motorInfeedSpeed = _instance.ReadFloat("DB_MixingLine.Line.MotorInfeed.SpeedAct");
                    float motorInfeedCurrent = _instance.ReadFloat("DB_MixingLine.Line.MotorInfeed.CurrentAct");
                    float mixerSpeed = _instance.ReadFloat("DB_MixingLine.Line.Mixer.Base.SpeedAct");
                    float mixerCurrent = _instance.ReadFloat("DB_MixingLine.Line.Mixer.Base.CurrentAct");
                    float outfeedSpeed = _instance.ReadFloat("DB_MixingLine.Line.MotorOutfeed.SpeedAct");
                    float outfeedCurrent = _instance.ReadFloat("DB_MixingLine.Line.MotorOutfeed.CurrentAct");
                    bool valveDosingOpen = _instance.ReadBool("DB_MixingLine.Line.ValveAdditiveDosing.OpenedFb");
                    bool valveDischargeOpen = _instance.ReadBool("DB_MixingLine.Line.ValveDischarge.OpenedFb");

                    // Step transition detection
                    if (step != currentStep)
                    {
                        if (currentStep >= 0)
                        {
                            report.StepTransitions.Add(new StepTransitionInfo
                            {
                                StepNumber = currentStep,
                                StepName = currentStepName,
                                DurationSeconds = Math.Round((DateTime.UtcNow - stepStartTime).TotalSeconds, 2)
                            });
                        }
                        currentStep = step;
                        currentStepName = stepName;
                        stepStartTime = DateTime.UtcNow;
                    }

                    // Record telemetry point
                    var sample = new MixingTelemetrySample
                    {
                        ElapsedSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2),
                        StepNumber = step,
                        StepName = stepName,
                        TankLevel = (float)Math.Round(tankLevel, 2),
                        MotorInfeedSpeed = (float)Math.Round(motorInfeedSpeed, 2),
                        MotorInfeedCurrent = (float)Math.Round(motorInfeedCurrent, 2),
                        MixerSpeed = (float)Math.Round(mixerSpeed, 2),
                        MixerCurrent = (float)Math.Round(mixerCurrent, 2),
                        OutfeedSpeed = (float)Math.Round(outfeedSpeed, 2),
                        OutfeedCurrent = (float)Math.Round(outfeedCurrent, 2),
                        ValveDosingOpened = valveDosingOpen,
                        ValveDischargeOpened = valveDischargeOpen
                    };
                    report.Telemetry.Add(sample);

                    // Peak value tracking
                    if (tankLevel > report.PeakTankLevel) report.PeakTankLevel = (float)Math.Round(tankLevel, 2);
                    if (motorInfeedCurrent > report.PeakInfeedCurrent) report.PeakInfeedCurrent = (float)Math.Round(motorInfeedCurrent, 2);
                    if (mixerSpeed > report.PeakMixerSpeed) report.PeakMixerSpeed = (float)Math.Round(mixerSpeed, 2);
                    if (mixerCurrent > report.PeakMixerCurrent) report.PeakMixerCurrent = (float)Math.Round(mixerCurrent, 2);
                    if (outfeedCurrent > report.PeakOutfeedCurrent) report.PeakOutfeedCurrent = (float)Math.Round(outfeedCurrent, 2);

                    if (fault)
                    {
                        faulted = true;
                        report.ErrorMessage = $"Line faulted during Step {step} ('{stepName}').";
                        break;
                    }

                    if (batchDone && step == 0 && sw.Elapsed.TotalSeconds > 1.0)
                    {
                        completed = true;
                        break;
                    }

                    Thread.Sleep(80);
                }

                if (currentStep >= 0)
                {
                    report.StepTransitions.Add(new StepTransitionInfo
                    {
                        StepNumber = currentStep,
                        StepName = currentStepName,
                        DurationSeconds = Math.Round((DateTime.UtcNow - stepStartTime).TotalSeconds, 2)
                    });
                }

                report.EndTime = DateTime.UtcNow;
                report.TotalDurationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 2);
                report.Success = completed && !faulted;

                if (!completed && !faulted)
                {
                    report.ErrorMessage = $"Execution timed out after {timeoutSeconds}s at Step {currentStep}.";
                }
            }
            finally
            {
                if (scaleFactor > 0 && Math.Abs(scaleFactor - prevScale) > 0.01)
                {
                    try { _instance!.ScaleFactor = prevScale; } catch { }
                }
            }

            return report;
        }
    }

    #region DTOs

    public class PlcSimInstanceSummary
    {
        public int ID { get; set; }
        public string Name { get; set; } = "";
        public string OperatingState { get; set; } = "";
        public string CPUType { get; set; } = "";
        public string IP { get; set; } = "";
    }

    public class PlcSimConnectResult
    {
        public bool Success { get; set; }
        public string InstanceName { get; set; } = "";
        public int ID { get; set; }
        public string OperatingState { get; set; } = "";
        public string OperatingMode { get; set; } = "";
        public string CPUType { get; set; } = "";
        public string IP { get; set; } = "";
        public double ScaleFactor { get; set; }
        public int TagCount { get; set; }
        public string Message { get; set; } = "";
    }

    public class PlcSimStatusResult
    {
        public string Name { get; set; } = "";
        public int ID { get; set; }
        public string OperatingState { get; set; } = "";
        public string OperatingMode { get; set; } = "";
        public string CPUType { get; set; } = "";
        public string IP { get; set; } = "";
        public double ScaleFactor { get; set; }
        public int TagCount { get; set; }
    }

    public class TagValueDto
    {
        public string TagName { get; set; } = "";
        public object? Value { get; set; }
        public string DataType { get; set; } = "";
    }

    public class MixingRecipeInput
    {
        public string RecipeID { get; set; } = "RECIPE_01";
        public float WaterLiters { get; set; } = 100.0f;
        public int AdditiveType { get; set; } = 1;
        public float AdditiveAmount { get; set; } = 20.0f;
        public float SugarKg { get; set; } = 10.0f;
        public float MixTimeSeconds { get; set; } = 3.0f;
        public float MixSpeedRpm { get; set; } = 1200.0f;
    }

    public class MixingSequenceRunReport
    {
        public bool Success { get; set; }
        public MixingRecipeInput Recipe { get; set; } = new MixingRecipeInput();
        public double ScaleFactor { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double TotalDurationSeconds { get; set; }
        public float PeakTankLevel { get; set; }
        public float PeakInfeedCurrent { get; set; }
        public float PeakMixerSpeed { get; set; }
        public float PeakMixerCurrent { get; set; }
        public float PeakOutfeedCurrent { get; set; }
        public string? ErrorMessage { get; set; }
        public List<StepTransitionInfo> StepTransitions { get; } = new List<StepTransitionInfo>();
        public List<MixingTelemetrySample> Telemetry { get; } = new List<MixingTelemetrySample>();
    }

    public class StepTransitionInfo
    {
        public int StepNumber { get; set; }
        public string StepName { get; set; } = "";
        public double DurationSeconds { get; set; }
    }

    public class MixingTelemetrySample
    {
        public double ElapsedSeconds { get; set; }
        public int StepNumber { get; set; }
        public string StepName { get; set; } = "";
        public float TankLevel { get; set; }
        public float MotorInfeedSpeed { get; set; }
        public float MotorInfeedCurrent { get; set; }
        public float MixerSpeed { get; set; }
        public float MixerCurrent { get; set; }
        public float OutfeedSpeed { get; set; }
        public float OutfeedCurrent { get; set; }
        public bool ValveDosingOpened { get; set; }
        public bool ValveDischargeOpened { get; set; }
    }

    public class SequenceExecutionInput
    {
        public string SequenceName { get; set; } = "Sequence_01";
        public string StartCommandTag { get; set; } = "";
        public string? ResetCommandTag { get; set; }
        public string StepNumberTag { get; set; } = "";
        public string? StepNameTag { get; set; }
        public string DoneTag { get; set; } = "";
        public string? FaultTag { get; set; }
        public int IdleStep { get; set; } = 0;
        public Dictionary<string, object>? InitialTags { get; set; }
        public Dictionary<string, object>? ParameterTags { get; set; }
        public List<string>? MonitoredTags { get; set; }
    }

    public class SequenceRunReport
    {
        public bool Success { get; set; }
        public string SequenceName { get; set; } = "";
        public double ScaleFactor { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double TotalDurationSeconds { get; set; }
        public string? ErrorMessage { get; set; }
        public List<StepTransitionInfo> StepTransitions { get; } = new List<StepTransitionInfo>();
        public List<Dictionary<string, object>> Telemetry { get; } = new List<Dictionary<string, object>>();
    }

    #endregion
}
