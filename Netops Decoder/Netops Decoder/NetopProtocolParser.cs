using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Netops_Decoder
{
    public class NetopProtocolParser
    {
        public SensorFunctions FindValueSensorFunctions(SensorFunctions[] sensorFunctionsArray, string value)
		{
            foreach (var data in sensorFunctionsArray)
            {
                if (data.Value == value)
                {
                    return data;
                }
            }
            
            return null;
        }

        public SensorBoardTypes FindValueSensorBoardTypes(SensorBoardTypes[] sensorBoardTypesArray, string value)
        {
            foreach (var data in sensorBoardTypesArray)
            {
                if (data.Value == value)
                {
                    return data;
                }
            }

            return null;
        }

        public DeviceInformationParameters FindValueDeviceInformationParameters(DeviceInformationParameters[] deviceInformationParametersArray,
            string value)
        {
            foreach (var data in deviceInformationParametersArray)
            {
                if (data.Value == value)
                {
                    return data;
                }
            }

            return null;
        }

		public object FindBitValue(Options option, Composer[] array, string value) //----- Need Help Ambiguous .BitValue
		{
            switch (option)
            {
                case Options.ActiveTimerValue:
                    ActiveTimerValue[] activeTimerValueArray = { };

                    foreach (var item in array)
                    {
                        activeTimerValueArray = item.ActiveTimerValue;
                    }

                    foreach (var data in activeTimerValueArray)
                    {
                        if (data.BitValue == value)
                        {
                            return data;
                        }
                    }
                    break;
                case Options.PeriodicTimerValue:
                    PeriodicTimerValue[] periodicTimerValueArray = { };

                    foreach (var item in array)
                    {
                        periodicTimerValueArray = item.PeriodicTimerValue;
                    }

                    foreach (var data in periodicTimerValueArray)
                    {
                        if (data.BitValue == value)
                        {
                            return data;
                        }
                    }
                    break;
                default:
                    Console.WriteLine("Wrong Option - FindValue()");
                    break;
            }

            return null;
		}

        public static string InsertAt(string rawData, string charToInsert, int position)
        {
            return rawData.Substring(0, position) + charToInsert + rawData.Substring(position); // slice implementation in JS
        }

        public Composer GetJson()
        {
            var path = "Protocol.json";
			var data = File.ReadAllText(path);
			var compose = new Composer();
			compose = JsonConvert.DeserializeObject<Composer>(data);
            return compose;
        }

        public string GetRawDataContent(string rawData)
        {
            var composer = GetJson();
			// InsertAt(rawData,"z",5); Check if the method is used anywhere in the class or program

            dynamic jsonResult = new ExpandoObject();
			var pointer = 2;
			var frameFormat = new FrameFormat();
			var calculations = new Calculations();

			//----------------------CheckSum Control------------------------//

			if (this.CheckSum((rawData)))
			{
				jsonResult.Success = false;
				jsonResult.Message = "CheckSum is false";
                var jsonResultString = JsonConvert.SerializeObject(jsonResult);
            }

			//----------------------Protocol Version------------------------//

            frameFormat.ProtocolHeader = calculations.Binary2Dec(calculations.Hex2Binary(rawData.Substring(0, 2)));
            if (frameFormat.ProtocolHeader != "1")
            {
                jsonResult.Success = false;
                jsonResult.Message = "Protocol Version is not supported! Taken version is" + frameFormat.ProtocolHeader;
                return JsonConvert.SerializeObject(jsonResult); // Suppose to be jsonResult --> change method return type from string
            }

			//----------------------Device Serial No------------------------//

			frameFormat.SerialNumber = rawData.Substring(pointer, 8);
			pointer += 8;
			frameFormat.DeviceType = calculations.Binary2Dec(calculations.GenericZeroAdd(calculations.Hex2Binary(frameFormat.SerialNumber)).Substring(0, 4));
			if (frameFormat.DeviceType != "1" &&
				frameFormat.DeviceType != "2" && frameFormat.DeviceType != "3" && frameFormat.DeviceType != "4")
			{
                jsonResult.Success = false;
				jsonResult.Message = "Unknown Device Type! Taken type is" + frameFormat.DeviceType;

				return JsonConvert.SerializeObject(jsonResult); // Suppose to be jsonResult --> change method return type from string
			}

            jsonResult.Success = true;
            jsonResult.SerialNo = frameFormat.SerialNumber;
            jsonResult.DeviceType = frameFormat.DeviceType;

            var list = new List<ExpandoObject>();
			var i = 0;

			while (pointer + 2 < rawData.Length)
			{

				//----------------------Data Header------------------------//	  
				var dataBlocks = new DataBlocks();
				var dataHeader = new DataHeader();
                dynamic dataBlockObject = new ExpandoObject(); // Alternative is JObject...
                if (pointer >= rawData.Length)
                {
                    dataBlocks.HeaderDataBinary = "0";
                }
                else
                {
					dataBlocks.HeaderDataBinary = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 6))); // changed 6 to 4
                    pointer += 6;
				}

                dataHeader.McrTime = dataBlocks.HeaderDataBinary.Substring(0, 1);

                if (dataBlocks.HeaderDataBinary.Length >= 2 && dataBlocks.HeaderDataBinary.Length >= 5)
                {
                    dataHeader.SlotNumber = calculations.Binary2Dec(dataBlocks.HeaderDataBinary.Substring(2, 3));
                }
                else
                {
                    dataHeader.SlotNumber = "0";

                }
				
                dataHeader.DpmhError = dataBlocks.HeaderDataBinary.Substring(5, 1);

                var HexSensorFunction = "";

				if (dataBlocks.HeaderDataBinary.Length >= 6 && dataBlocks.HeaderDataBinary.Length >= 15)
                {
                    HexSensorFunction = calculations.Binary2Hex(dataBlocks.HeaderDataBinary.Substring(6, 9)).ToUpper();
                    HexSensorFunction = HexSensorFunction.Substring(HexSensorFunction.Length - 1, 1) == "0"
                        ? HexSensorFunction.Substring(0, HexSensorFunction.Length - 1) : HexSensorFunction;
				}
                else
                {
                    HexSensorFunction = "0";
                }

                var BinSensorBoardType = "";

				if (dataBlocks.HeaderDataBinary.Length >= 16 && dataBlocks.HeaderDataBinary.Length >= 24)
                { 
                    BinSensorBoardType = calculations.Binary2Hex(dataBlocks.HeaderDataBinary.Substring(16, 8)).ToUpper();
                }
                else
                {
                    BinSensorBoardType = "0";
                }

                dataHeader.SensorFunction = "0x0" + HexSensorFunction;
				dataHeader.SensorBoardType = "0x0" + BinSensorBoardType.ToUpper();

				if (dataHeader.SlotNumber == "0" && dataHeader.SensorBoardType == "0x00")
				{
					dataHeader.SensorFunctionStr = "";
					dataHeader.SensorBoardTypeStr = "";
				}
				else
                {
                    dataHeader.SensorFunctionStr = (FindValueSensorFunctions(composer.SensorFunctions, 
                        dataHeader.SensorFunction))
                        .Description;
					dataHeader.SensorBoardTypeStr = (FindValueSensorBoardTypes(composer.SensorBoardTypes, 
                        dataHeader.SensorBoardType))
                        .Description;
				}

                dataBlockObject.Timestamp_Exists = dataHeader.McrTime;
                dataBlockObject.Slot_No = dataHeader.SlotNumber;
                dataBlockObject.Error_Bit = dataHeader.DpmhError;
                dataBlockObject.Sensor_Function = dataHeader.SensorFunctionStr + "(" + calculations.Hex2Decimal(HexSensorFunction) + ")";
                dataBlockObject.Sensor_Board_Type = dataHeader.SensorBoardTypeStr + "(" + calculations.Hex2Decimal(BinSensorBoardType) + ")";

                dataBlockObject.Sensor_Function_Value = calculations.Hex2Decimal(HexSensorFunction);
                dataBlockObject.Sensor_Board_Type_Value = calculations.Hex2Decimal(BinSensorBoardType);

                //----------------------Device Information table control-----------------------//
				if (dataHeader.SlotNumber == "0" && dataHeader.SensorBoardType == "0x00")
				{
					foreach (var data in composer.DeviceInformationParameters)
					{
						if (data.Value == dataHeader.SensorFunction)
						{
							switch (data.Value)
							{
								case "0x00":
									{
										dataBlockObject[data.Description] = calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2));
										pointer += data.PayloadLength * 2;
										break;
									}
								case "0x008":
									{
										dataBlockObject[data.Description] = calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2));
										pointer += data.PayloadLength * 2;
										break;
									}
								case "0x005":
									{
										var result = "v" + calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2)).Insert(1, ".");
										dataBlockObject[data.Description] = result;
										pointer += data.PayloadLength * 2;
										break;
									}
								case "0x006":
									{
										var result = "v" + calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2)).Insert(1, ".");
										dataBlockObject[data.Description] = result;
										pointer += data.PayloadLength * 2;
										break;
									}
								case "0x00C":
									{
										if (Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2))) == 0)
											dataBlockObject[data.Description] = "Access technology is not using eDRX mode";
										else
											dataBlockObject[data.Description] = " Access technology supports eDRX mode";

										pointer += data.PayloadLength * 2;
										break;
									}
								case "0x0E":
									{
										var cal = calculations.GenericZeroAdd(calculations.Dec2Binary(Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2))))).Substring(0, 3);
										var periodicTimerValue = (PeriodicTimerValue)FindBitValue(Options.PeriodicTimerValue, composer.PeriodicTimerValue, cal);
										var time = calculations.GenericZeroAdd(calculations.Dec2Binary(Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2))))).Substring(3, 5);
										if (periodicTimerValue.BitValue == "111")
                                            dataBlockObject[data.Description] = periodicTimerValue.Timer;
										else
                                            dataBlockObject[data.Description] = (Convert.ToInt32(calculations.Binary2Dec(time)) * Convert.ToInt32(periodicTimerValue.Value)) + " " + periodicTimerValue.Timer;

										pointer += data.PayloadLength * 2;
										break;
									}

								case "0x00F":
									{
										var activeTimerValue = (ActiveTimerValue)FindBitValue(Options.ActiveTimerValue, composer.ActiveTimerValue, calculations.GenericZeroAdd(calculations.Dec2Binary(Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2))))).Substring(0, 3));
										var time = calculations.GenericZeroAdd(calculations.Dec2Binary(Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2))))).Substring(3, 5);

										if (activeTimerValue.BitValue == "111")
                                            dataBlockObject[data.Description] = activeTimerValue.Timer;
										else
                                            dataBlockObject[data.Description] = (Convert.ToInt32(calculations.Binary2Dec(time)) * Convert.ToInt32(activeTimerValue.Value)) + " " + activeTimerValue.Timer;

										pointer += data.PayloadLength * 2;
										break;
									}
								case "0x010":
									var hes = Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, data.PayloadLength * 2)));
                                    dataBlockObject[data.Description] = hes.ToString();
									if (hes <= 100)
										dataBlockObject["Charging"] = "false";
									else if (hes >= 100 || hes < 255)
                                        dataBlockObject["Charging"] = "true";
									else
										dataBlockObject["Charging"] = "Battery measurement not supported";
									pointer += data.PayloadLength * 2;
									break;
								default:
                                    dataBlockObject[data.Description] = calculations.Hex2Decimal(rawData.Substring(pointer,
                                        data.PayloadLength * 2));
									pointer += data.PayloadLength * 2;
									break;
							}
						}
					}//end foreach
				}
				//----------------------Slot Informations------------------------//
				else if (dataHeader.SlotNumber != "0" && dataHeader.SensorBoardType == "0x00")
				{
                    string key = "";
					switch (dataHeader.SensorFunction)
                    {
                        case "0x03":
                            key = "slot" + dataHeader.SlotNumber + "_serial_no";
                            dataBlockObject[key] = rawData.Substring(pointer, 8);
							pointer += 8;
							break;
						case "0x05": 
							key = "slot" + dataHeader.SlotNumber + "_sw_version";
							dataBlockObject[key]= calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							break;
						case "0x06":
							key = "slot" + dataHeader.SlotNumber + "_hw_version";
							dataBlockObject[key] = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							break;
					}
				}
				//----------------------Sensor Informations------------------------//
				if (dataHeader.McrTime == "1")
				{
					var unixTime = calculations.UnixTimeStampToDateTime(Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, 8))));
					pointer += 8;
					dataBlockObject.datetime_utc = unixTime;
				}
				//-------------------Sensor Functions---------------------// 
				switch (dataHeader.SensorFunction)
				{
					case "0x09"://-- Temperature Sensor
						{
							var temperature = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 3))) - 4000) / 100;
							dataBlockObject["temperature"] = temperature;
							pointer += 3;
							break;
						}
					case "0x0A"://-- Humidity  Sensor
						{
							var humidity = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 10;
							dataBlockObject["humidity"] = humidity;
							pointer += 4;
							break;
						}
					case "0x0B"://Humidity&&Temperature Sensor
						{
							var tmp = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 6)));
							pointer += 6;
							var temperature = (float.Parse(calculations.Binary2Dec(tmp.Substring(0, 14))) - 4000) / 100;
							var humidity = (float.Parse(calculations.Binary2Dec(tmp.Substring(14, 10)))) / 10;
							dataBlockObject.temperature = temperature;
							dataBlockObject.humidity = humidity;
							break;
						}
					case "0x026": //-- 3 Axis Accelerometer Sensor  
						{
							var acciX = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 1000;
							pointer += 4;
							var acciY = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 1000;
							pointer += 4;
							var acciZ = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 1000;
							pointer += 4;
							dataBlockObject.acceleration_x = acciX;
							dataBlockObject.acceleration_y = acciY;
							dataBlockObject.acceleration_z = acciZ;
							break;
						}
					case "0x03": // Vibration Sensor
						{
							var vibration = 0;
							vibration = Convert.ToInt32(float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 100;
							pointer += 4;
							dataBlockObject["vibration"] = vibration;
							break;
						}
					case "0x023"://--  Barometric Pressure Sensor
						{
							var barometer = calculations.Hex2Decimal(rawData.Substring(pointer, 6));
							pointer += 6;
							dataBlockObject["barometer "] = barometer;
							break;
						}
					case "0x01": //Shaking Sensor
                    {
                            var accX = 0f;
                            var accY = 0f; 
                            var accZ = 0f;
                            if (pointer >= rawData.Length)
                            {
                                accX = 0;
                            }
                            else
                            {
                                accX = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 1000;
                                pointer += 4;
                            }

                            if (pointer >= rawData.Length)
                            {
                                accY = 0;
                            }
                            else
                            {
								accY = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 1000;
                                pointer += 4;
							}
                            if (pointer >= rawData.Length)
                            {
                                accZ = 0;
                            }
                            else
                            {
								accZ = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 1000;
						        pointer += 4;
							}
						    dataBlockObject.acceleration_x = accX;
						    dataBlockObject.acceleration_y = accY;
						    dataBlockObject.acceleration_z = accZ;
						    break;
                    }
					case "0x02": // 2 Plane Tilt Detection Sensor
                    {
                            var tiltX = 0;
                            var tiltY = 0;
							tiltX = Convert.ToInt32(float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 100;
							pointer += 4;
							tiltY = Convert.ToInt32(float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 100;
							pointer += 4;
							dataBlockObject["tilt_x"] = tiltX;
							dataBlockObject["tilt_y"] = tiltY;
							break;
						}
					case "0x04": // 1 Phase Current Sensor
						{
							var phase_1 = 0;
							phase_1 = Convert.ToInt32(float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 10;
							pointer += 4;
							dataBlockObject["phase_10"] = phase_1;
							break;
						}
					case "0x05": // 3 Phase Current Sensor
						{
							var phase1 = 0;
                            var phase2 = 0;
                            var phase3 = 0;
							phase1 = Convert.ToInt32(float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 10;
							pointer += 4;
							phase2 = Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 10;
							pointer += 4;
							phase3 = Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 10;
							pointer += 4;
							dataBlockObject["phase_1"] = phase1;
							dataBlockObject["phase_2"] = phase2;
							dataBlockObject["phase_3"] = phase3;
							break;
						}
					case "0x06":  // Dry Contact Sensor
						{
							var inBin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							dataBlockObject["in_1"] = inBin.Substring(7, 1) == "0" ? "asserted" : "not-asserted";
							dataBlockObject["in_2"] = inBin.Substring(6, 1) == "0" ? "asserted" : "not-asserted";
							dataBlockObject["in_3"] = inBin.Substring(5, 1) == "0" ? "asserted" : "not-asserted";
							dataBlockObject["in_4"] = inBin.Substring(4, 1) == "0" ? "asserted" : "not-asserted";
							break;
						}
					case "0x0C"://-- Door Counter Sensor 
						{
							var openingCounter = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							var closingCounter = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							dataBlockObject["Opening Counter"] = openingCounter;
							dataBlockObject["Close Counter"] = closingCounter;
							break;
						}
					case "0x0D"://-- 3 Plane Tilt Detection Sensor 
						{
							var tilX = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 100;
							pointer += 4;
							var tilY = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 100;
							pointer += 4;
							var tilZ = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 100;
							pointer += 4;
							dataBlockObject["Til X"] = tilX;
							dataBlockObject["Til Y"] = tilY;
							dataBlockObject["Til Z"] = tilZ;
							break;
						}
					case "0x0E"://-- 1 Button Sensor 
						{
							var button_Bin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							dataBlockObject["button_pressed"] = button_Bin;
							break;
						}
					case "0x00F"://-- 3 Button Sensor 
						{
							var buttonBin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							var button_pressed_1 = buttonBin.Substring(8, 1);
							var button_pressed_2 = buttonBin.Substring(7, 1);
							var button_pressed_3 = buttonBin.Substring(6, 1);
							dataBlockObject["button_pressed_1"] = button_pressed_1;
							dataBlockObject["button_pressed_2"] = button_pressed_2;
							dataBlockObject["button_pressed_3"] = button_pressed_3;
							break;
						}
					case "0x011"://-- RTD Temperature Sensor 
						{
							var temperature = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 2000) / 10;
							dataBlockObject["temperature"] = temperature;
							pointer += 4;
							break;
						}
					case "0x012"://-- PIR Sensor 
						{
							var pirBBin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							var pir = pirBBin.Substring(8, 1);
							dataBlockObject["pir"] = pir;
							break;
						}
					case "0x013"://-- Distance Sensor
						{
							var distance = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							dataBlockObject["distance"] = distance;
							break;
						}
					case "0x014"://-- Ambient Light Sensor 
						{
							var ambient = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 8)))) / 100;
							pointer += 8;
							dataBlockObject["ambient"] = ambient;
							break;
						}
					case "0x016"://-- Glass Break Sensor 
						{
							var glass = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							dataBlockObject["glass_break"] = glass;
							break;
						}
					case "0x017"://-- 1 Phase Mains Voltage Sensor
						{
							var phase = (float.Parse(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							dataBlockObject["Voltage Value of Phase 1"] = phase;
							break;
						}
					case "0x018"://-- 3 Phase Mains Voltage Sensor
						{
							var vphase1 = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							var vphase2 = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							var vphase3 = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							dataBlockObject["Voltage Value of Phase 1"] = vphase1;
							dataBlockObject["Voltage Value of Phase 2"] = vphase2;
							dataBlockObject["Voltage Value of Phase 3"] = vphase3;
							break;
						}
					case "0x01C"://-- 1 Phase Mains Voltage Sensor with Power Cut Detection
						{
							var mvphase = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							dataBlockObject["Voltage Value of Phase 1"] = mvphase;
							break;
						}
					case "0x01D"://-- 3 Phase Mains Voltage Sensor with Power Cut Detection 
						{
							var mvphase1 = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							var mvphase2 = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							var mvphase3 = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) / 100;
							pointer += 4;
							dataBlockObject["Voltage Value of Phase 1"] = mvphase1;
							dataBlockObject["Voltage Value of Phase 2"] = mvphase2;
							dataBlockObject["Voltage Value of Phase 3"] = mvphase3;
							break;
						}
					case "0x01F"://--Soil Moisture Sensor
						{
							var soilMoisture = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 2)));
							pointer += 4;
							dataBlockObject["Soil Moisture"] = soilMoisture;
							break;
						}
					case "0x020"://-- Manhole Sensor
						{
							var manholebin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							var manhole = manholebin.Substring(8, 1);
							dataBlockObject["manhole"] = manhole;
							break;
						}
					case "0x021"://--	Magnetic Mesurement Sensor 
						{
							var pm1 = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							var pm2 = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							var pm3 = calculations.Hex2Decimal(rawData.Substring(pointer, 4));
							pointer += 4;
							dataBlockObject["Magnetic Field of X Axis"] = pm1;
							dataBlockObject["Magnetic Field of Y Axis"] = pm2;
							dataBlockObject["Magnetic Field of Z Axis"] = pm3;
							break;
						}
					case "0x022": //--   Power Line Analyzer Sensor
						{
							var PhA_Voltage = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Current = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_EffectiveCurrent = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Frequency = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_pf = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_kW = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_kVAr = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Thd_I = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Thd_U = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Harmonic_3 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Harmonic_5 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Harmonic_7 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Harmonic_9 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhA_Harmonic_11 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;

							var PhB_Voltage = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Current = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_EffectiveCurrent = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Frequency = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_pf = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_kW = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_kVAr = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Thd_I = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Thd_U = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Harmonic_3 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Harmonic_5 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Harmonic_7 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Harmonic_9 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhB_Harmonic_11 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;

							var PhC_Voltage = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Current = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_EffectiveCurrent = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Frequency = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_pf = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_kW = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_kVAr = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Thd_I = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Thd_U = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Harmonic_3 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Harmonic_5 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Harmonic_7 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Harmonic_9 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;
							var PhC_Harmonic_11 = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;

							var Notr = float.Parse(calculations.GenericZeroAdd(calculations.Hex2Binary(calculations.ReverseString(rawData.Substring(pointer, 8)))));
							pointer += 8;

							dataBlockObject["Pha_Voltage"] = PhA_Voltage;
							dataBlockObject["Pha_Current"] = PhA_Current;
							dataBlockObject["Pha_EffectiveCurrent"] = PhA_EffectiveCurrent;
							dataBlockObject["Pha_Frequency"] = PhA_Frequency;
							dataBlockObject["Pha_Pf"] = PhA_pf;
							dataBlockObject["Pha_kW"] = PhA_kW;
							dataBlockObject["Pha_kVAr"] = PhA_kVAr;
							dataBlockObject["Pha_Thd_I"] = PhA_Thd_I;
							dataBlockObject["Pha_Thd_U"] = PhA_Thd_U;
							dataBlockObject["Pha_Harmonic_3"] = PhA_Harmonic_3;
							dataBlockObject["Pha_Harmonic_5"] = PhA_Harmonic_5;
							dataBlockObject["Pha_Harmonic_7"] = PhA_Harmonic_7;
							dataBlockObject["Pha_Harmonic_9"] = PhA_Harmonic_9;
							dataBlockObject["Pha_Harmonic_11"] = PhA_Harmonic_11;

							dataBlockObject["Phb_Voltage"] = PhB_Voltage;
							dataBlockObject["Phb_Current"] = PhB_Current;
							dataBlockObject["Phb_EffectiveCurrent"] = PhB_EffectiveCurrent;
							dataBlockObject["Phb_Frequency"] = PhB_Frequency;
							dataBlockObject["Phb_Pf"] = PhB_pf;
							dataBlockObject["Phb_kW"] = PhB_kW;
							dataBlockObject["Phb_kVAr"] = PhB_kVAr;
							dataBlockObject["Phb_Thd_I"] = PhB_Thd_I;
							dataBlockObject["Phb_Thd_U"] = PhB_Thd_U;
							dataBlockObject["Phb_Harmonic_3"] = PhB_Harmonic_3;
							dataBlockObject["Phb_Harmonic_5"] = PhB_Harmonic_5;
							dataBlockObject["Phb_Harmonic_7"] = PhB_Harmonic_7;
							dataBlockObject["Phb_Harmonic_9"] = PhB_Harmonic_9;
							dataBlockObject["Phb_Harmonic_11"] = PhB_Harmonic_11;

							dataBlockObject["Phc_Voltage"] = PhC_Voltage;
							dataBlockObject["Phc_Current"] = PhC_Current;
							dataBlockObject["Phc_EffectiveCurrent"] = PhC_EffectiveCurrent;
							dataBlockObject["Phc_Frequency"] = PhC_Frequency;
							dataBlockObject["Phc_Pf"] = PhC_pf;
							dataBlockObject["Phc_kW"] = PhC_kW;
							dataBlockObject["Phc_kVAr"] = PhC_kVAr;
							dataBlockObject["Phc_Thd_I"] = PhC_Thd_I;
							dataBlockObject["Phc_Thd_U"] = PhC_Thd_U;
							dataBlockObject["Phc_Harmonic_3"] = PhC_Harmonic_3;
							dataBlockObject["Phc_Harmonic_5"] = PhC_Harmonic_5;
							dataBlockObject["Phc_Harmonic_7"] = PhC_Harmonic_7;
							dataBlockObject["Phc_Harmonic_9"] = PhC_Harmonic_9;
							dataBlockObject["Phc_Harmonic_11"] = PhC_Harmonic_11;

							dataBlockObject["INotr"] = Notr;
							break;
						}
					case "0x024"://-- Tilt Switch Sensor
						{
							var tilt = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2))).Substring(8, 1);
							if (tilt == "0")
							{
								dataBlockObject["UnActivated"]="True";
							}
							else
							{
								dataBlockObject["Activated"]="False";
							}
							pointer += 2;
							break;
						}
					case "0x025"://-- Parking Lot Sensor.
						{
							var parkBin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							var park = parkBin.Substring(8, 1);
							dataBlockObject["value"] = park;
							break;
						}
					case "0x027"://--  6 Axis Accelerometer Sensor
						{
							var acciX6 = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 1000;
							pointer += 4;
							var acciY6 = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 1000;
							pointer += 4;
							var acciZ6 = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4)))) / 1000;
							pointer += 4;
                            var acciTX6 = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 1000;
							pointer += 4;
							var acciTY6 = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 1000;
							pointer += 4;
							var acciTZ6 = (float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 4))) - 9000) / 1000;
							pointer += 4;
							dataBlockObject["acceleration_x"] = acciX6;
							dataBlockObject["acceleration_y"] = acciY6;
							dataBlockObject["acceleration_z"] = acciZ6;
							dataBlockObject["tilt_x"] = acciTX6;
							dataBlockObject["tilt_y"] = acciTY6;
							dataBlockObject["tilt_z"] = acciTZ6;
							break;
						}
					case "0x028"://-- Power Line Analyzer Sensor – Model 2 
						{
							var P1_Voltage = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 8)));
							pointer += 8;
							var P1_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_Frequency = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_Power_Factor = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_Active_Power = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_Reactive_Power = float.Parse(calculations.Hex2Decimal(rawData.Substring(pointer, 8)));
							pointer += 8;
							var P1_THD_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_THD_Voltage = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_3rd_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_5th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_7th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_9th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_11th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_13rd_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_15th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_17th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P1_19th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P111th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;

							var P2_Voltage = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_Frequency = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_Power_Factor = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_Active_Power = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_Reactive_Power = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_THD_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_THD_Voltage = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_3rd_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_5th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_7th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_9th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_11th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_13rd_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_15th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_17th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P2_19th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P211th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;

							var P3_Voltage = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_Frequency = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_Power_Factor = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_Active_Power = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_Reactive_Power = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_THD_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_THD_Voltage = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_3rd_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_5th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_7th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_9th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_11th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_13rd_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_15th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_17th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P3_19th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var P311th_Harmonic_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;
							var Neutral_Current = calculations.Hex2Decimal(rawData.Substring(pointer, 8));
							pointer += 8;

							dataBlockObject["P1 Voltage (RMS) "] = P1_Voltage;
							dataBlockObject["P1 Current (RMS) "] = P1_Current;
							dataBlockObject["P1 Frequency "] = P1_Frequency;
							dataBlockObject["P1 Power Factor "] = P1_Power_Factor;
							dataBlockObject["P1 Active Power "] = P1_Active_Power;
							dataBlockObject["P1 Reactive Power "] = P1_Reactive_Power;
							dataBlockObject["P1 THD on Current "] = P1_THD_Current;
							dataBlockObject["P1 THD on Voltage "] = P1_THD_Voltage;
							dataBlockObject["P1 3rd Harmonic of Current "] = P1_3rd_Harmonic_Current;
							dataBlockObject["P1 5th Harmonic of Current "] = P1_5th_Harmonic_Current;
							dataBlockObject["P1 7th Harmonic of Current "] = P1_7th_Harmonic_Current;
							dataBlockObject["P1 9th Harmonic of Current "] = P1_9th_Harmonic_Current;
							dataBlockObject["P1 11th Harmonic of Current "] = P1_11th_Harmonic_Current;
							dataBlockObject["P13rd Harmonic of Current (Abs) "] = P1_13rd_Harmonic_Current;
							dataBlockObject["P15th Harmonic of Current (Abs) "] = P1_15th_Harmonic_Current;
							dataBlockObject["P17th Harmonic of Current (Abs) "] = P1_17th_Harmonic_Current;
							dataBlockObject["P19th Harmonic of Current (Abs) "] = P1_19th_Harmonic_Current;
							dataBlockObject["P111th Harmonic of Current (Abs) "] = P111th_Harmonic_Current;

							dataBlockObject["P2 Voltage (RMS) "] = P2_Voltage;
							dataBlockObject["P2 Current (RMS) "] = P2_Current;
							dataBlockObject["P2 Frequency "] = P2_Frequency;
							dataBlockObject["P2 Power Factor "] = P2_Power_Factor;
							dataBlockObject["P2 Active Power "] = P2_Active_Power;
							dataBlockObject["P2 Reactive Power "] = P2_Reactive_Power;
							dataBlockObject["P2 THD on Current "] = P2_THD_Current;
							dataBlockObject["P2 THD on Voltage "] = P2_THD_Voltage;
							dataBlockObject["P2 3rd Harmonic of Current "] = P2_3rd_Harmonic_Current;
							dataBlockObject["P2 5th Harmonic of Current "] = P2_5th_Harmonic_Current;
							dataBlockObject["P2 7th Harmonic of Current "] = P2_7th_Harmonic_Current;
							dataBlockObject["P2 9th Harmonic of Current "] = P2_9th_Harmonic_Current;
							dataBlockObject["P2 11th Harmonic of Current "] = P2_11th_Harmonic_Current;
							dataBlockObject["P2 3rd Harmonic of Current (Abs) "] = P2_13rd_Harmonic_Current;
							dataBlockObject["P2 5th Harmonic of Current (Abs) "] = P2_15th_Harmonic_Current;
							dataBlockObject["P2 7th Harmonic of Current (Abs) "] = P2_17th_Harmonic_Current;
							dataBlockObject["P2 9th Harmonic of Current (Abs) "] = P2_19th_Harmonic_Current;
							dataBlockObject["P211th Harmonic of Current (Abs) "] = P211th_Harmonic_Current;

							dataBlockObject["P3 Voltage (RMS) "] = P3_Voltage;
							dataBlockObject["P3 Current (RMS) "] = P3_Current;
							dataBlockObject["P3 Frequency "] = P3_Frequency;
							dataBlockObject["P3 Power Factor "] = P3_Power_Factor;
							dataBlockObject["P3 Active Power "] = P3_Active_Power;
							dataBlockObject["P3 Reactive Power "] = P3_Reactive_Power;
							dataBlockObject["P3 THD on Current "] = P3_THD_Current;
							dataBlockObject["P3 THD on Voltage "] = P3_THD_Voltage;
							dataBlockObject["P3 3rd Harmonic of Current "] = P3_3rd_Harmonic_Current;
							dataBlockObject["P3 5th Harmonic of Current "] = P3_5th_Harmonic_Current;
							dataBlockObject["P3 7th Harmonic of Current "] = P3_7th_Harmonic_Current;
							dataBlockObject["P3 9th Harmonic of Current "] = P3_9th_Harmonic_Current;
							dataBlockObject["P3 11th Harmonic of Current "] = P3_11th_Harmonic_Current;
							dataBlockObject["P3 3rd Harmonic of Current (Abs) "] = P3_13rd_Harmonic_Current;
							dataBlockObject["P3 5th Harmonic of Current (Abs) "] = P3_15th_Harmonic_Current;
							dataBlockObject["P3 7th Harmonic of Current (Abs) "] = P3_17th_Harmonic_Current;
							dataBlockObject["P3 9th Harmonic of Current (Abs) "] = P3_19th_Harmonic_Current;
							dataBlockObject["P311th Harmonic of Current (Abs) "] = P311th_Harmonic_Current;

							dataBlockObject["Neutral_Current"] = Neutral_Current;
							break;
						}
                    case "0x029"://-- Water Flooding Sensor
						{
							var Water_Flooding_Sensor = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2))).Substring(8, 1);
							pointer += 2;
							if (Water_Flooding_Sensor == "1")
							{
								dataBlockObject["There is flooding water"] = 1;
							}
							else
							{
								dataBlockObject["There isn't flooding water"] = 0;
							}
							break;
						}
					case "0x02A"://-- Water Level Threshold Sensor 
						{
							var Threshold = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 1))).Substring(7, 0);
							pointer += 1;
							if (Threshold == "0")
							{
								dataBlockObject["Water Level Status"] = Threshold;
							}
							else
							{
								dataBlockObject["Water Level Status"] = Threshold;
							}
							break;
						}
					case "0x02C"://-- Waste Bin Sensor 
						{
							var Waste_Bin = calculations.Hex2Decimal(rawData.Substring(pointer, 3));
							pointer += 3;
							dataBlockObject["Distance to waste surface: "] = Waste_Bin;
							break;
						}
					case "0x010"://-- Door Sensor 
						{
							var doorBin = calculations.GenericZeroAdd(calculations.Hex2Binary(rawData.Substring(pointer, 2)));
							pointer += 2;
							var door = doorBin.Substring(7, 1);
							dataBlockObject["door"] = door;
							break;
						}
				}
				list.Add(dataBlockObject);
			}//end While
			jsonResult.data_blocks = list.ToArray();
		//	document.getElementById("cod").innerHTML = JSON.stringify(jsonResult, null, " ");
            jsonResult = JsonConvert.SerializeObject(jsonResult);// Just come back to double check if we should add the JSON string in a new string variable or not
            return jsonResult.ToString();
        }

        //Checking the accuracy of the data with the Checksum algorithm.
        public bool CheckSum(string rawData)
        {
            var calculations = new Calculations();
            var hexSum = 0;
            for (var i = 0; i < rawData.Length - 2; i += 2)
            {
                hexSum = hexSum + Convert.ToInt32(calculations.Hex2Decimal(rawData.Substring(i, 2)));
            }
            var allBitValue = calculations.Hex2Binary(calculations.Dec2Hex(hexSum.ToString()));
            var maskBitValue = calculations.Hex2Decimal(calculations.Binary2Hex(allBitValue.Substring(allBitValue.Length - 8, 8)));
            var zeroEkle = calculations.ZeroAdd(calculations.Dec2Binary(~Convert.ToInt32(maskBitValue, 10)));
            var comp = calculations.BitComplement(zeroEkle);
            var topla = calculations.OneAddBinary(comp);
            var decimals = calculations.Binary2Dec(topla);
            var hex = calculations.Dec2Hex(decimals);
            if (hex.Substring(hex.Length - 2, 2) == rawData.Substring(rawData.Length - 2, 2))
            {
                Console.WriteLine(true);
                return false;
            }
            else
            {
                Console.WriteLine(false);
                return true;
            }
        }
	}
}
