using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Threading;

namespace CoreControllers
{
    /// <summary>
    /// Класс для работы с контроллером
    /// </summary>
    class Controller
    {
        #region Конструктор класса

        /// <summary>
        /// Инициализация класса
        /// </summary>
        public Controller()
        {
            _isConnected = false;
            keepLink = false;
            STATUS = EnumStatusDevice.Off;
            PS = new CurentModeParameters();

            SETTING = new Setting
            {
                ResetControllerInConnect = true,
                PortName          = @"",
                PortSpeed         = 115200,
                CountAxes         = AxisVariant.XYZ,


                pos_Machine       = new Position(0, 0, 0),
                pos_Works         = new Position(0, 0, 0),
                pos_WCO           = new Position(0, 0, 0),
                pos_ProbePin      = new Position(0, 0, 0),
                pos_G54 = new Position(0, 0, 0),
                pos_G55 = new Position(0, 0, 0),
                pos_G56 = new Position(0, 0, 0),
                pos_G57 = new Position(0, 0, 0),
                pos_G58 = new Position(0, 0, 0),
                pos_G59 = new Position(0, 0, 0),
                pos_G28 = new Position(0, 0, 0),
                pos_G30 = new Position(0, 0, 0),
                pos_G92 = new Position(0, 0, 0),
                pos_TLO = new Position(0, 0, 0),
                Gcode_ParserState = new CurentModeParameters(),
                IntervalRefreshPos = 100,
                IntervalRefreshStatus = 4000







            };

            //DataExchangeManager = new MessageManager();
            //EnableEventTouchPin = false;

            DM = new DataManager();

            //CalcSizeBuffer = 0;
            SETTING.buffer_Size = 15;
            SETTING.IntervalRefreshPos = 100;
        }

        #endregion

        #region Переменные

        /// <summary>
        /// Статус наличия связи с контроллером
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected; }
        }

        private bool _isConnected;

        /// <summary>
        /// необходимость удержания связи с контроллером
        /// </summary>
        volatile private bool keepLink;

        /// <summary>
        /// Текущий статус контроллера
        /// </summary>
        public EnumStatusDevice STATUS;

        /// <summary>
        /// Параметры установленных режимов контроллера
        /// </summary>
        public CurentModeParameters PS;

        /// <summary>
        /// Настройки GRBL контроллера
        /// </summary>
        public Setting SETTING;

        /// <summary>
        /// Поток работы с контроллером
        /// </summary>
        private BackgroundWorker thWorker;

        /// <summary>
        /// Модуль хранения сообщений
        /// </summary>
        public DataManager DM;


        /// <summary>
        /// Для синхронизации операций
        /// </summary>
        static object locker = new object();

        //private int CalcSizeBuffer;

        public int SizeBufferSend()
        {
            lock (locker)
            {
                return SETTING.buffer_Size;
                //return CalcSizeBuffer;
            }

        }




        #endregion

        #region события для уведомления подписчиков

        /// <summary>
        /// Параметр для передачи с событием
        /// </summary>
        public class EventVariantStatusMessageArgs : EventArgs
        {
            /// <summary>
            /// Команда
            /// </summary>
            public eVariantStatusMessage Value;
            /// <summary>
            /// Описание
            /// </summary>
            public string Descriptions;

            public EventVariantStatusMessageArgs(eVariantStatusMessage _Value, string _Descriptions = "")
            {
                this.Value = _Value;
                this.Descriptions = _Descriptions;
            }
        }

        /// <summary>
        /// Событие о новых данных от контроллера
        /// </summary>
        public event EventHandler EvStatusMessage;
        
        /// <summary>
        /// Параметр события, если нужно передать команду
        /// </summary>
        public class EventСmdArgs : EventArgs
        {
            /// <summary>
            /// Команда
            /// </summary>
            public DataCommand Value;
            /// <summary>
            /// Источник команды
            /// </summary>
            public eVariantDataMessage variant;
            /// <summary>
            /// Значение от реалтайма
            /// </summary>
            public byte RTvalue;

            public string strValue;

            public EventСmdArgs(DataCommand _Value, eVariantDataMessage _variant, byte _RTvalue = 0, string _strValue = "")
            {
                this.Value = _Value;
                this.variant = _variant;
                this.RTvalue = _RTvalue;
                this.strValue = _strValue;
            }
        }

        //--------------

        /// <summary>
        /// Событие о новых данных от контроллера
        /// </summary>
        public event EventHandler EvDataMessage;

        /// <summary>
        /// Событие изменения режимов работы контроллера
        /// </summary>
        public event EventHandler EvRefreshCurentModeParameters;

        #region События для SendINFO
        
        /// <summary>
        /// Событие для передачи сообщений от ядра контроллера
        /// </summary>
        public event EventInfo evInfo;
        public delegate void EventInfo(object sender, EventArgsInfo e);

        /// <summary>
        /// Аргументы для события
        /// </summary>
        public class EventArgsInfo
        {
            public string Message { get; private set; }
            public InfoMessageStatus status { get; private set; }

            public EventArgsInfo(string str, InfoMessageStatus indx)
            {
                Message = str;
                status = indx;
            }
        }

        public enum InfoMessageStatus
        {
            Normal,
            Warning,
            Error
        }

        private void SendInfo(string Message, InfoMessageStatus status = InfoMessageStatus.Normal)
        {
            EventInfo handler = evInfo;
            handler?.Invoke(this, new EventArgsInfo(DateTime.Now.ToString("g") + " | " + Message, status));
        }


        #endregion

        #region События датчика касания

        /// <summary>
        /// Событие при получении события от датчика касания
        /// </summary>
        public event EventHandler EvTouchPin;

        /// <summary>
        /// Активация срабатывания датчика касания
        /// Активируется только от посылаемого кода G38.2
        /// </summary>
        //private bool EnableEventTouchPin;

        #endregion

        #endregion

        #region Запуск подключения / отключения
        
        /// <summary>
        /// Запуск подключения к контроллеру
        /// </summary>
        /// <param name="PortName">Наименование ком-порта</param>
        /// <param name="PortSpeed">Скорость обмена с портом</param>
        public void Connect(string PortName = "", int PortSpeed = 115200, bool ResetInConnect = true)
        {
            SETTING.ResetControllerInConnect = ResetInConnect;
            SETTING.PortName  = PortName;
            SETTING.PortSpeed = PortSpeed;

            SendInfo(@"Вызов запуска потока работы с контроллером...");

            // инициализая потока работы с контроллером
            thWorker = new BackgroundWorker();
            thWorker.WorkerSupportsCancellation = true;
            thWorker.DoWork             += ThWorker_DoWork;
            thWorker.RunWorkerCompleted += ThWorker_RunWorkerCompleted;
            thWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Выполнение процедуры отключения от контроллера
        /// </summary>
        public void Disconect()
        {
            keepLink = false;
            SendInfo(@"Вызов остановки потока работы с контроллером...");
        }

        #endregion

        #region Ключевой поток контроллера

        private void ThWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            SendInfo(@"Поток запущен");

            SerialPort _comPort = new SerialPort(SETTING.PortName, SETTING.PortSpeed);

            bool _lostConnect = false;

            try
            {
                _comPort.Open();
                SendInfo("Установлено подключение к com-порту!");

                if (SETTING.ResetControllerInConnect)
                {
                    _comPort.DtrEnable = true;
                    _comPort.DtrEnable = false;
                    SendInfo("Выполнена перезагрузка GRBL контроллера, согласно настройкам.");
                }
                _comPort.DataReceived += _comPort_DataReceived;
                _isConnected = true;
                //событие что удалось подключиться
                EventHandler handler = EvStatusMessage;
                handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.Connected));
            }
            catch (Exception exception)
            {
                SendInfo("Подключиться к ком-порту НЕ ПОЛУЧИЛОСЬ!!! по причине: " + exception.Message);
                _isConnected = false;
                //событие что не удалось подключиться
                EventHandler handler = EvStatusMessage;
                handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.Disconnected,exception.Message));
                return;
            }

            keepLink = true;
            DM = new DataManager();
            
            //SendCommand("$10=2", eSourceData.refreshInfo); //настройка посылки координат, и размера буфера  (2- базовые рабочие координаты   (3-машинные
            //SendCommand("$I"   , eSourceData.refreshInfo); // запрос даты прошивки
            //SendCommand("$G"   , eSourceData.refreshInfo); // запрос статусов...
            //SendRealTimeCommand(ControllerRealTimeCommand.StatusReportQuery);

            int _curTimeInterval    = 0;//для запроса координат, скоростей
            int _curTimeParseStatus = 0; //для получения отпарсенных режимов контроллера

            while (keepLink)
            {
                // пауза в 1 мс, что-бы не занимать сильно в холостую процессорное время
                Thread.Sleep(1);

                // отсылка срочных комманд
                while (DM.AvaibleRealTimeCommand)
                {
                    try
                    {
                        byte bb = DM.GetRealtimeCommandFromStack();
                        if (bb == 0) continue;
                        byte[] tmp = new[] { bb };

                        lock (locker)
                        {
                            if (SETTING.buffer_Size > 0) SETTING.buffer_Size--;
                            //CalcSizeBuffer--;
                        }
                        _comPort.Write(tmp, 0, 1);
                        
                        //Debug.WriteLine("--> write byte:" + bb);
                    }
                    catch (Exception exception)
                    {
                        //Прервем подключение...
                        keepLink = false;
                        SendInfo("ОШИБКА!!!!!! -  отправки данных! причина: " + exception.Message);
                        SendInfo("Остановка цикла отправки!");

                        //событие что потеряли связь с контроллером
                        EventHandler handler = EvStatusMessage;
                        handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.LostConnect));
                        _lostConnect = true;
                        continue;
                    }
                    Thread.Sleep(1); // но с маленькой паузой
                }




                // отсылка срочных комманд
                while (DM.AvaibleRealTimeCommandString)
                {
                    try
                    {
                        string ss = DM.GetRealtimeCommandStringFromStack();
                        if (ss == "") continue;
                        

                        lock (locker)
                        {
                            if (SETTING.buffer_Size > 0) SETTING.buffer_Size--;
                            //CalcSizeBuffer--;
                        }
                        _comPort.WriteLine(ss);

                        //Debug.WriteLine("--> write byte:" + bb);
                    }
                    catch (Exception exception)
                    {
                        //Прервем подключение...
                        keepLink = false;
                        SendInfo("ОШИБКА!!!!!! -  отправки данных! причина: " + exception.Message);
                        SendInfo("Остановка цикла отправки!");

                        //событие что потеряли связь с контроллером
                        EventHandler handler = EvStatusMessage;
                        handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.LostConnect));
                        _lostConnect = true;
                        continue;
                    }
                    Thread.Sleep(1); // но с маленькой паузой
                }








                if (DM.AvaibleCommand && SETTING.buffer_Size  > 10)  // ранее было это  SETTING.buffer_Size ///CalcSizeBuffer
                {
                    string ss = DM.GetCommandForSends();

                    try
                    {

                        lock (locker)
                        {
                            if (SETTING.buffer_Size > 0) SETTING.buffer_Size--;

                            //CalcSizeBuffer--;
                        }
                        _comPort.WriteLine(ss);
                        
                        DM.AcceptCommandForSend();

                        DataCommand dc = DM.GetCommandSending();
                        EventHandler handler = EvDataMessage;
                        handler?.Invoke(this, new EventСmdArgs(dc,eVariantDataMessage.CommandSend));


                    }
                    catch (Exception exception)
                    {
                        //Прервем подключение...
                        keepLink = false;
                        SendInfo("ОШИБКА!!!!!! -  отправки данных! причина: " + exception.Message);
                        SendInfo("Остановка цикла отправки!");

                        //событие что потеряли связь с контроллером
                        EventHandler handler = EvStatusMessage;
                        handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.LostConnect));
                        _lostConnect = true;
                        continue;
                    }
                }

                if (SETTING.IntervalRefreshPos != 0)
                {
                    _curTimeInterval++;

                    if (_curTimeInterval > SETTING.IntervalRefreshPos)
                    {
                        SendRealTimeCommand(ControllerRealTimeCommand.StatusReportQuery);
                        _curTimeInterval = 0;
                    }
                }

                if (SETTING.IntervalRefreshStatus != 0)
                {
                    _curTimeParseStatus++;

                    if (_curTimeParseStatus >= SETTING.IntervalRefreshStatus)
                    {
                        SendCommand(ControllerCommand.query_Parsing);
                        _curTimeParseStatus = 0;
                    }
                }


            }











            if (_comPort.IsOpen && !_lostConnect)
            {
                _comPort.Close();
                SendInfo("Закрытие связи с ком-портом!");
                _comPort.DataReceived -= _comPort_DataReceived;
            }
            SendInfo(@"Окончание работы потока");

            STATUS = EnumStatusDevice.Off;
        }

        private void ThWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                SendInfo(@"Принудительная остановка потока.");
            }

            else if (e.Error != null)
            {
                SendInfo(@"Остановка потока в связи с ошибкой.");
            }
            else
            {
                SendInfo(@"Нормальное завершение потока.");
                SendInfo(@"---------------------------------------------");
            }

            _isConnected = false;
            //событие что удалось отключиться
            EventHandler handler = EvStatusMessage;
            handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.Disconnected));
        }
        
        #endregion
        
        #region Добавление получение сообщений


        /// <summary>
        /// Тут копится набор данных из буфера
        /// </summary>
        private string BUFFER_DATA = "";

        /// <summary>
        /// Событие от порта о получении новых данных
        /// </summary>
        private void _comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort spL = (SerialPort)sender;
            byte[] buf = new byte[spL.BytesToRead];
            //Console.WriteLine("DATA RECEIVED!");
            spL.Read(buf, 0, buf.Length);
            foreach (Byte b in buf)
            {
                BUFFER_DATA += (char)b;
            }

            // полученные данные разобъем на строки
            string[] arrString = BUFFER_DATA.Split('\n');

            if (arrString.Length == 0) return;

            foreach (string ss in arrString)
            {

                string s = ss.ToUpper();

                if (s.EndsWith("\r"))
                {
                    //удаляем перенос строки
                    string tmps = s.Substring(0, s.Length - 1).ToUpper();
                    // извлекаем данные из строки
                    ParseCommand(tmps);  
                }
                else
                {
                    // по какой-то причине это лишь часть строки...
                    BUFFER_DATA = s;
                }
            }
        }

        /// <summary>
        /// Добавление суппер команды для отправки
        /// </summary>
        /// <param name="_bb"></param>
        public void SendRealTimeCommand(ControllerRealTimeCommand _val)
        {
            byte tmp = (byte) _val;
            DM.AddRealtimeCommand(tmp);
            EventHandler handler = EvDataMessage;
            handler?.Invoke(this, new EventСmdArgs(null,eVariantDataMessage.CommandRTAdd,tmp));
        }



        /// <summary>
        /// Добавление суппер команды для отправки
        /// </summary>
        /// <param name="_bb"></param>
        public void SendRealTimeCommand(ControllerCommand _val)
        {
            string strCommand;

            switch (_val)
            {
                case ControllerCommand.SLEEP:
                    strCommand = "$SLP";
                    break;
                case ControllerCommand.query_POS:
                    strCommand = "$#";
                    break;
                case ControllerCommand.KillAlarmLock:
                    strCommand = "$X";
                    break;
                case ControllerCommand.Reset_ControllerSetting:
                    strCommand = "$RST=$";
                    break;
                case ControllerCommand.Reset_Coordinates:
                    strCommand = "$RST=#";
                    break;
                case ControllerCommand.Reset_All:
                    strCommand = "$RST=*";
                    break;
                case ControllerCommand.TestMode:
                    strCommand = "$C";
                    break;
                case ControllerCommand.AboutFirmware:
                    strCommand = "$I";
                    break;
                case ControllerCommand.ToHome:
                    strCommand = "$H";
                    break;
                case ControllerCommand.query_Parsing:
                    strCommand = "$G";
                    break;
                case ControllerCommand.query_setting:
                    strCommand = "$$";
                    break;
                default:
                    return;
            }

            DM.AddRealtimeCommand(strCommand);
            EventHandler handler = EvDataMessage;
            handler?.Invoke(this, new EventСmdArgs(null, eVariantDataMessage.CommandRTAdd, 0, strCommand));
        }

        /// <summary>
        /// Добавление суппер команды для отправки
        /// </summary>
        /// <param name="_bb"></param>
        public void SendRealTimeCommand(string _val)
        {
            DM.AddRealtimeCommand(_val);
            EventHandler handler = EvDataMessage;
            handler?.Invoke(this, new EventСmdArgs(null, eVariantDataMessage.CommandRTAdd, 0, _val));
        }


        /// <summary>
        /// Посылка предоопределенных комманд контроллеру
        /// </summary>
        /// <param name="_str"></param>
        public void SendCommand(ControllerCommand _cmd)
        {
            
            //CalcSizeBuffer--;
            string strCommand;

            switch (_cmd)
            {
                case ControllerCommand.SLEEP:
                    strCommand = "$SLP";
                    break;
                case ControllerCommand.query_POS:
                    strCommand = "$#";
                    break;
                case ControllerCommand.KillAlarmLock:
                    strCommand = "$X";
                    break;
                case ControllerCommand.Reset_ControllerSetting:
                    strCommand = "$RST=$";
                    break;
                case ControllerCommand.Reset_Coordinates:
                    strCommand = "$RST=#";
                    break;
                case ControllerCommand.Reset_All:
                    strCommand = "$RST=*";
                    break;
                case ControllerCommand.TestMode:
                    strCommand = "$C";
                    break;
                case ControllerCommand.AboutFirmware:
                    strCommand = "$I";
                    break;
                case ControllerCommand.ToHome:
                    strCommand = "$H";
                    break;
                case ControllerCommand.query_Parsing:
                    strCommand = "$G";
                    break;
                default:
                    return;
            }

            DM.AddSendCommand(strCommand,eSourceData.appCommand);
            DataCommand dc = DM.GetCommandForSend();
            EventHandler handler = EvDataMessage;
            handler?.Invoke(this, new EventСmdArgs(dc,eVariantDataMessage.CommandAdd));
        }

        /// <summary>
        /// Добавление команды для посылки
        /// </summary>
        /// <param name="_str"></param>
        public void SendCommand(string _str, eSourceData source)
        {
            DM.AddSendCommand(_str, source);

            DataCommand dc = DM.GetCommandForSend();

            EventHandler handler = EvDataMessage;
            handler?.Invoke(this, new EventСmdArgs(dc,eVariantDataMessage.CommandAdd));
        }



        #endregion

        #region Получение развернутого описания

        /// <summary>
        /// Получение описания аварийной ситуации
        /// </summary>
        public string GetAlarmDescription(string _codeAlarm)
        {
            string sValue = "";

            string codeAlarm = _codeAlarm.ToUpper().Replace("ALARM:", "");

            switch (codeAlarm)
            {
                case "1":
                    sValue = "Hard limit triggered.Machine position is likely lost due to sudden and immediate halt. Re - homing is highly recommended.";
                    break;
                case "2":
                    sValue = "G - code motion target exceeds machine travel.Machine position safely retained. Alarm may be unlocked.";
                    break;
                case "3":
                    sValue = "Reset while in motion.Grbl cannot guarantee position. Lost steps are likely. Re - homing is highly recommended.";
                    break;
                case "4":
                    sValue = "Probe fail. The probe is not in the expected initial state before starting probe cycle, where G38.2 and G38.3 is not triggered and G38.4 and G38.5 is triggered.";
                    break;
                case "5":
                    sValue = "Probe fail. Probe did not contact the workpiece within the programmed travel for G38.2 and G38.4.";
                    break;
                case "6":
                    sValue = "Homing fail.Reset during active homing cycle.";
                    break;
                case "7":
                    sValue = "Homing fail.Safety door was opened during active homing cycle.";
                    break;
                case "8":
                    sValue = "Homing fail.Cycle failed to clear limit switch when pulling off. Try increasing pull - off setting or check wiring.";
                    break;
                case "9":
                    sValue = "Homing fail. Could not find limit switch within search distance. Defined as 1.5 * max_travel on search and 5 * pulloff on locate phases.";
                    break;


                default: break;
            }

            return sValue;
        }

        /// <summary>
        /// Получение описания ошибки посылки команды в парсер
        /// </summary>
        public string GetErrorDescription(string _codeError)
        {

            string sValue = "";

            string codeError = _codeError.ToUpper().Replace("ERROR:", "");

            switch (codeError)
            {
                case "1": sValue = "Послана команда, которую не может распознать парсер контроллера, проверьте правильность посланной команды!"; break; // G-code words consist of a letter and a value. Letter was not found.
                case "2": sValue = "Numeric value format is not valid or missing an expected value."; break;
                case "3": sValue = "Grbl '$' system command was not recognized or supported."; break;
                case "4": sValue = "Negative value received for an expected positive value."; break;
                case "5": sValue = "Homing cycle is not enabled via settings."; break;
                case "6": sValue = "Minimum step pulse time must be greater than 3usec"; break;
                case "7": sValue = "EEPROM read failed. Reset and restored to default values."; break;
                case "8": sValue = "Grbl '$' command cannot be used unless Grbl is IDLE. Ensures smooth operation during a job."; break;
                case "9": sValue = "G-code locked out during alarm or jog state."; break;
                case "10": sValue = "Soft limits cannot be enabled without homing also enabled."; break;
                case "11": sValue = "Max characters per line exceeded. Line was not processed and executed."; break;
                case "12": sValue = "(Compile Option) Grbl '$' setting value exceeds the maximum step rate supported."; break;
                case "13": sValue = "Safety door detected as opened and door state initiated."; break;
                case "14": sValue = "(Grbl-Mega Only) Build info or startup line exceeded EEPROM line length limit."; break;
                case "15": sValue = "Jog target exceeds machine travel. Command ignored."; break;
                case "16": sValue = "Jog command with no '=' or contains prohibited g-code."; break;
                case "17": sValue = "Laser mode requires PWM output."; break;
                // ?!? 18,19
                case "20": sValue = "Unsupported or invalid g-code command found in block."; break;
                case "21": sValue = "More than one g-code command from same modal group found in block."; break;
                case "22": sValue = "Feed rate has not yet been set or is undefined."; break;
                case "23": sValue = "G-code command in block requires an integer value."; break;
                case "24": sValue = "Two G-code commands that both require the use of the XYZ axis words were detected in the block."; break;
                case "25": sValue = "A G-code word was repeated in the block."; break;
                case "26": sValue = "A G-code command implicitly or explicitly requires XYZ axis words in the block, but none were detected."; break;
                case "27": sValue = "N line number value is not within the valid range of 1 - 9,999,999."; break;
                case "28": sValue = "A G-code command was sent, but is missing some required P or L value words in the line."; break;
                case "29": sValue = "Grbl supports six work coordinate systems G54-G59. G59.1, G59.2, and G59.3 are not supported."; break;
                case "30": sValue = "The G53 G-code command requires either a G0 seek or G1 feed motion mode to be active. A different motion was active."; break;
                case "31": sValue = "There are unused axis words in the block and G80 motion mode cancel is active."; break;
                case "32": sValue = "A G2 or G3 arc was commanded but there are no XYZ axis words in the selected plane to trace the arc."; break;
                case "33": sValue = "The motion command has an invalid target. G2, G3, and G38.2 generates this error, if the arc is impossible to generate or if the probe target is the current position."; break;
                case "34": sValue = "A G2 or G3 arc, traced with the radius definition, had a mathematical error when computing the arc geometry. Try either breaking up the arc into semi-circles or quadrants, or redefine them with the arc offset definition."; break;
                case "35": sValue = "A G2 or G3 arc, traced with the offset definition, is missing the IJK offset word in the selected plane to trace the arc."; break;
                case "36": sValue = "There are unused, leftover G-code words that aren't used by any command in the block."; break;
                case "37": sValue = "The G43.1 dynamic tool length offset command cannot apply an offset to an axis other than its configured axis. The Grbl default axis is the Z-axis."; break;


                default: break;
            }




            return sValue;

        }

        

        #endregion

        #region Парсинг полученных данных
        
        /// <summary>
        /// Разбор полученных данных
        /// </summary>
        /// <param name="_svalue"></param>
        private void ParseCommand(string _svalue)
        {
            if (_svalue == "OK")
            {
                DM.AddReceiveCommand(_svalue); //поместим в стек полученную комманду
                DataCommand dc = DM.GetCommandRecived();
                EventHandler handler1 = EvDataMessage;
                handler1?.Invoke(this, new EventСmdArgs(dc, eVariantDataMessage.CommandRecived));
                return;
            }
            
            if (_svalue.StartsWith("ERROR:"))
            {
                DM.AddReceiveCommand(_svalue); //поместим в стек полученную комманду
                DataCommand dc = DM.GetCommandRecived();
                if (dc != null)
                {
                    dc.ResultCommand += " " + GetErrorDescription(_svalue);
                    EventHandler handler2 = EvDataMessage;
                    handler2?.Invoke(this, new EventСmdArgs(dc, eVariantDataMessage.CommandRecived));
                }

                return;
            }

            if (_svalue.StartsWith("ALARM:"))
            {
                DM.AddReceiveOtherCommand(_svalue + " " + GetAlarmDescription(_svalue)); //поместим в стек полученную комманду
                EventHandler handler3 = EvDataMessage;
                handler3?.Invoke(this, new EventСmdArgs(null, eVariantDataMessage.CommandRecivedOther, 0, _svalue + " " + GetAlarmDescription(_svalue)));
                return;
            }

            //TODO: временно пока тут
            DM.AddReceiveOtherCommand(_svalue); //поместим в стек полученную комманду
            EventHandler handler4 = EvDataMessage;
            handler4?.Invoke(this, new EventСmdArgs(null, eVariantDataMessage.CommandRecivedOther,0,_svalue));
            
            //TODO: ------

            if (_svalue.StartsWith("<") && _svalue.EndsWith(">"))
            {
                ParseStatusReport(_svalue);
                return;
            }

            if (_svalue.StartsWith("[") && _svalue.EndsWith("]"))
            {
                ParsePropertyReport(_svalue);
                return;
            }

            if (_svalue.StartsWith("$"))
            {
                // от контроллера прилетела информация о параметре
                ParseProperty(_svalue);
            }
            
            //SendInfo(@"<---- " + _svalue);
        }

        /// <summary>
        /// Парсинг строки параметров
        /// </summary>
        private void ParseProperty(string _svalue)
        {
            string[] sa = _svalue.Split('=');

            if (sa[0] == "$0")
            {
                int.TryParse(sa[1], out SETTING.p00_StepPulse);
            }
            if (sa[0] == "$1")
            {
                int.TryParse(sa[1], out SETTING.p01_StepIdleDelay);
            }
            if (sa[0] == "$2")
            {
                int.TryParse(sa[1], out SETTING.p02_StepPortInvertMask);
            }
            if (sa[0] == "$3")
            {
                int.TryParse(sa[1], out SETTING.p03_DirectionPortInvertMask);
            }
            if (sa[0] == "$4")
            {
                int.TryParse(sa[1], out SETTING.p04_StepEnableInvert);
            }
            if (sa[0] == "$5")
            {
                int.TryParse(sa[1], out SETTING.p05_LimitPinsInvert);
            }
            if (sa[0] == "$6")
            {
                int.TryParse(sa[1], out SETTING.p06_ProbePinInvert);
            }
            if (sa[0] == "$10")
            {
                int.TryParse(sa[1], out SETTING.p10_StatusReport);
            }

            //11 и 12 дробный параметр


            if (sa[0] == "$13")
            {
                int.TryParse(sa[1], out SETTING.p13_ReportInches);
            }

            if (sa[0] == "$20")
            {
                int.TryParse(sa[1], out SETTING.p20_SoftLimit);
            }

            if (sa[0] == "$21")
            {
                int.TryParse(sa[1], out SETTING.p21_HardLimit);
            }



            //13,20,21,22,23 - целый

            //24,25 - дробный

            //26 целый

            //27 - дробный

            //30 - целый

            // 31,32 - целый

            //100,101,102,103,110,111,112,113,120,121,122,123,130,131,132,133 - дробные

            string _decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (sa[0] == "$11") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p11_JunctionDeviation);
            if (sa[0] == "$12") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p12_ArcTolerance);

            if (sa[0] == "$100") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p100_StepX);
            if (sa[0] == "$101") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p101_StepY);
            if (sa[0] == "$102") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p102_StepZ);
            if (sa[0] == "$103") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p103_StepA);
            if (sa[0] == "$104") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p104_StepB);

            if (sa[0] == "$110") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p110_MaxRateX);
            if (sa[0] == "$111") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p111_MaxRateY);
            if (sa[0] == "$112") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p112_MaxRateZ);
            if (sa[0] == "$113") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p113_MaxRateA);
            if (sa[0] == "$114") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p114_MaxRateB);

            if (sa[0] == "$120") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p120_AccelerationX);
            if (sa[0] == "$121") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p121_AccelerationY);
            if (sa[0] == "$122") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p122_AccelerationZ);
            if (sa[0] == "$123") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p123_AccelerationA);
            if (sa[0] == "$124") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p124_AccelerationB);

            if (sa[0] == "$130") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p130_MaxTravelX);
            if (sa[0] == "$131") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p131_MaxTravelY);
            if (sa[0] == "$132") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p132_MaxTravelZ);
            if (sa[0] == "$133") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p133_MaxTravelA);
            if (sa[0] == "$134") decimal.TryParse(sa[1].Replace(".", _decimalSeparator), out SETTING.p134_MaxTravelB);

            EventHandler handler = EvStatusMessage;
            handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.Setting));
        }

        /// <summary>
        /// Парсинг статусной строки
        /// </summary>
        /// <param name="textToParse">Строка для парсинга</param>
        private void ParseStatusReport(string textToParse)
        {
            // Возможные варианты:
            // <Idle|MPos:0.000,-25.000,-97.000|Bf:15,127|FS:0,0>
            // <Idle|MPos:0.000,-25.000,-97.000|Bf:15,127|FS:0,0|Ov:100,100,100>
            // <Idle|MPos:0.000,-25.000,-97.000|Bf:15,127|FS:0,0|WCO:0.000,0.000,0.000>

            // тут сработал датчик касания "Pn:P"   значение P - probe,  может быть ещё XYZ
            // <Run|MPos:0.000,0.000,-1.133|Bf:14,65|FS:0,0|Pn:P|Ov:100,100,100>

            //<ALARM|WPOS:0.000,0.000,0.000,0.000|BF:35,254|FS:0,0|PN:XYZA>


            // Удалим крайние символы, и разобьем строку на список строк
            string[] massivString = textToParse.Replace("<", "").Replace(">", "").ToUpper().Split('|');
            
            bool ChangeFinded = false;
            
            EnumStatusDevice oldStatus = STATUS;
            
            string sStatus = massivString[0];

            if (sStatus.StartsWith("IDLE" )) STATUS = EnumStatusDevice.Idle;
            if (sStatus.StartsWith("RUN"  )) STATUS = EnumStatusDevice.Run;
            if (sStatus.StartsWith("HOLD" )) STATUS = EnumStatusDevice.Hold;
            if (sStatus.StartsWith("DOOR" )) STATUS = EnumStatusDevice.Door;
            if (sStatus.StartsWith("HOME" )) STATUS = EnumStatusDevice.Home;
            if (sStatus.StartsWith("ALARM")) STATUS = EnumStatusDevice.Alarm;
            if (sStatus.StartsWith("CHECK")) STATUS = EnumStatusDevice.Check;
            if (sStatus.StartsWith("SLEEP")) STATUS = EnumStatusDevice.Sleep;
            if (sStatus.StartsWith("JOG"))   STATUS = EnumStatusDevice.JOG;

            if (STATUS != oldStatus)
            {
                ChangeFinded = true; // Статус то поменялся!
            }

            string mainPosSystem = ""; // для определения того какие координаты считать базовыми

            if (massivString[1].ToUpper().StartsWith("MPOS")) mainPosSystem = "MPOS"; else mainPosSystem = "WPOS";
            
            foreach (string sm in massivString)
            {
                string sValue = sm.Trim();

                if (sValue.StartsWith("MPOS") || sValue.StartsWith("WPOS") || sValue.StartsWith("WCO"))
                {
                    // If WPos: is given, use MPos = WPos + WCO.
                    // If MPos: is given, use WPos = MPos - WCO.
                    int ind = 0;

                    if (sValue.StartsWith("MPOS") || sValue.StartsWith("WPOS")) ind = 5;
                    else ind = 4;

                    Position tempPOS = new Position(sValue.Substring(ind));

                    bool needCalcWorkPos = false;
                    bool needCalcMachPos = false;

                    if (sValue.StartsWith("WPOS") && mainPosSystem == "WPOS")
                    {
                        if (!tempPOS.CompareWith(SETTING.pos_Works))  // несовпадение!!! поменялись координаты
                        {
                            SETTING.pos_Works = tempPOS;
                            ChangeFinded      = true;
                            needCalcMachPos   = true;
                        }
                    }

                    if (sValue.StartsWith("MPOS") && mainPosSystem == "MPOS")
                    {
                        if (!tempPOS.CompareWith(SETTING.pos_Machine))  // несовпадение!!! поменялись координаты
                        {
                            SETTING.pos_Machine = tempPOS;
                            ChangeFinded        = true;
                            needCalcWorkPos     = true;
                        }
                    }

                    if (sValue.StartsWith("WCO"))
                    {
                        if (!tempPOS.CompareWith(SETTING.pos_WCO))  // несовпадение!!! поменялись координаты
                        {
                            SETTING.pos_WCO = tempPOS;
                            ChangeFinded    = true;

                            //а так-же пересчитаем подчиненные координаты

                            //calc machine pos
                            if (mainPosSystem == "WPOS") needCalcMachPos = true;
                            
                            // calc work pos
                            if (mainPosSystem == "MPOS") needCalcWorkPos = true;
                        }
                    }

                    if (needCalcWorkPos)
                    {
                        decimal xw = SETTING.pos_Machine.X - SETTING.pos_WCO.X;
                        decimal yw = SETTING.pos_Machine.Y - SETTING.pos_WCO.Y;
                        decimal zw = SETTING.pos_Machine.Z - SETTING.pos_WCO.Z;
                        decimal aw = SETTING.pos_Machine.A - SETTING.pos_WCO.A;
                        decimal bw = SETTING.pos_Machine.B - SETTING.pos_WCO.B;

                        Position tempWPOS = new Position(xw,yw,zw,aw,bw);

                        if (!tempWPOS.CompareWith(SETTING.pos_Works))
                        {
                            SETTING.pos_Works = tempWPOS;
                            ChangeFinded = true;
                        }
                    }

                    if (needCalcMachPos)
                    {
                        decimal xm = SETTING.pos_Works.X + SETTING.pos_WCO.X;
                        decimal ym = SETTING.pos_Works.Y + SETTING.pos_WCO.Y;
                        decimal zm = SETTING.pos_Works.Z + SETTING.pos_WCO.Z;
                        decimal am = SETTING.pos_Works.A + SETTING.pos_WCO.A;
                        decimal bm = SETTING.pos_Works.B + SETTING.pos_WCO.B;

                        Position tempMPOS = new Position(xm,ym,zm,am,bm);

                        if (!tempMPOS.CompareWith(SETTING.pos_Machine))
                        {
                            SETTING.pos_Machine = tempMPOS;
                            ChangeFinded = true;
                        }
                    }
                }
                
                if (sValue.StartsWith("BF"))
                {
                    //Bf:15,127

                    string[] sArray = sValue.Substring(3).Split(',');

                    int iTmp = 0;

                    int.TryParse(sArray[0], out iTmp);

                    if (SETTING.buffer_Size != iTmp)
                    {

                        lock (locker)
                        {
                            SETTING.buffer_Size = iTmp;
                        }
                        

                        ChangeFinded = true;
                    }
              
                    int.TryParse(sArray[1], out iTmp);
                    SETTING.buffer_RX = iTmp;

                }

                if (sValue.StartsWith("FS"))
                {
                    //FS:0,0
                    string[] sarray = sValue.Substring(3).Split(',');

                    int iSpeed = 0;
                    int iPower = 0;

                    int.TryParse(sarray[0], out iSpeed);
                    int.TryParse(sarray[1], out iPower);

                    if (SETTING.Speed != iSpeed)
                    {
                        SETTING.Speed = iSpeed;
                        ChangeFinded = true;
                    }

                    if (SETTING.Power != iPower)
                    {
                        SETTING.Power = iPower;
                        ChangeFinded = true;
                    }
                }

                if (sValue.StartsWith("OV"))
                {
                    //Ov:100,100,100
                    string[] sarray = sValue.Substring(3).Split(',');

                    int iFeed = 0;
                    int iRapid = 0;
                    int iPower = 0;

                    int.TryParse(sarray[0], out iFeed);
                    int.TryParse(sarray[1], out iRapid);
                    int.TryParse(sarray[2], out iPower);

                    if (SETTING.OverrideFeed != iFeed)
                    {
                        SETTING.OverrideFeed = iFeed;
                        ChangeFinded = true;
                    }

                    if (SETTING.OverrideRapid != iRapid)
                    {
                        SETTING.OverrideRapid = iRapid;
                        ChangeFinded = true;
                    }

                    if (SETTING.OverridePower != iPower)
                    {
                        SETTING.OverridePower = iPower;
                        ChangeFinded = true;
                    }
                }

                if (sValue.StartsWith("PN:"))
                {
                    if (sValue == "PN:XYZA" && SETTING.CountAxes != AxisVariant.XYZA)
                    {
                        SETTING.CountAxes = AxisVariant.XYZA;
                        ChangeFinded = true;
                    }


                    if (sValue == "PN:XYZAB" && SETTING.CountAxes != AxisVariant.XYZAB)
                    {
                        SETTING.CountAxes = AxisVariant.XYZAB;
                        ChangeFinded = true;
                    }
                }



            } // foreach (string s in massivString)

            SETTING.pos_MAIN = mainPosSystem;

            if (!ChangeFinded) return; //изменений не произошло.......

            EventHandler handler = EvStatusMessage;
            handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.mainReport));
        }


        /// <summary>
        /// Парсинг сообщений в квадратных скобках
        /// </summary>
        private void ParsePropertyReport(string textToParse)
        {

            if (textToParse.StartsWith("[PRB:") )//&& EnableEventTouchPin
            {
                // Варианты данных
                // [PRB:0.000,1.208,0.000:1]
                // [PRB:-104.000,-67.000,-6.340:1]
                string val = textToParse.Replace("[PRB:", "").Replace("]", "");
                string[] val2 = val.Split(':');

                Position tempPOSProbe = new Position(val2[0]);

                if (SETTING.pos_MAIN == "MPOS")
                {
                    SETTING.pos_ProbePin = tempPOSProbe;
                }
                else
                {
                    //SETTING.pos_ProbePin = tempPOSProbe;

                    //WPOS
                    decimal xm = tempPOSProbe.X - SETTING.pos_Machine.X + SETTING.pos_Works.X;
                    decimal ym = tempPOSProbe.Y - SETTING.pos_Machine.Y + SETTING.pos_Works.Y;
                    decimal zm = tempPOSProbe.Z - SETTING.pos_Machine.Z + SETTING.pos_Works.Z;
                    decimal am = tempPOSProbe.A - SETTING.pos_Machine.A + SETTING.pos_Works.A;

                    SETTING.pos_ProbePin = new Position(xm, ym, zm, am);
                }
                //EnableEventTouchPin = false; //выключаем реакцию

                EventHandler handler = EvTouchPin;
                handler?.Invoke(this, new EventArgs());
                return;
            }

            if (textToParse.StartsWith("[GC:"))
            {
                // [GC: G0 G54 G17 G21 G90 G94 M5 M9 T0 F0 S0]
                CurentModeParameters tmps = new CurentModeParameters(textToParse);

                if (!PS.Compare(tmps) )
                {
                    PS = tmps;
                    EventHandler handler = EvRefreshCurentModeParameters;
                    handler?.Invoke(this, new EventArgs());
                }
            }

            //Примеры:
            //[G54:4.000,0.000,0.000]
            //[G55:4.000,6.000,7.000]
            //[G56:0.000,0.000,0.000]
            //[G57:0.000,0.000,0.000]
            //[G58:0.000,0.000,0.000]
            //[G59:0.000,0.000,0.000]
            //[G28:1.000,2.000,0.000]
            //[G30:4.000,6.000,0.000]
            //[G92:0.000,0.000,0.000]
            //[TLO:0.000]
            //[PRB:0.000,0.000,0.000:0]

            if (textToParse.StartsWith("[G54:"))
            {
                string stmp = textToParse.Replace("[G54:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G54.CompareWith(tmppos))
                {
                    SETTING.pos_G54 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG54));
                    return;
                }
            }

            if (textToParse.StartsWith("[G55:"))
            {
                string stmp = textToParse.Replace("[G55:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G55.CompareWith(tmppos))
                {
                    SETTING.pos_G55 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG55));
                    return;
                }
            }

            if (textToParse.StartsWith("[G56:"))
            {
                string stmp = textToParse.Replace("[G56:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G56.CompareWith(tmppos))
                {
                    SETTING.pos_G56 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG56));
                    return;
                }
            }

            if (textToParse.StartsWith("[G57:"))
            {
                string stmp = textToParse.Replace("[G57:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G57.CompareWith(tmppos))
                {
                    SETTING.pos_G57 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG57));
                    return;
                }
            }

            if (textToParse.StartsWith("[G58:"))
            {
                string stmp = textToParse.Replace("[G58:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G58.CompareWith(tmppos))
                {
                    SETTING.pos_G58 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG58));
                    return;
                }
            }

            if (textToParse.StartsWith("[G59:"))
            {
                string stmp = textToParse.Replace("[G59:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G59.CompareWith(tmppos))
                {
                    SETTING.pos_G59 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG59));
                    return;
                }
            }






            if (textToParse.StartsWith("[G28:"))
            {
                string stmp = textToParse.Replace("[G28:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G28.CompareWith(tmppos))
                {
                    SETTING.pos_G28 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG28));
                    return;
                }
            }

            if (textToParse.StartsWith("[G30:"))
            {
                string stmp = textToParse.Replace("[G30:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G30.CompareWith(tmppos))
                {
                    SETTING.pos_G30 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG30));
                    return;
                }
            }

            if (textToParse.StartsWith("[G92:"))
            {
                string stmp = textToParse.Replace("[G92:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_G92.CompareWith(tmppos))
                {
                    SETTING.pos_G92 = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posG92));
                    return;
                }
            }

            if (textToParse.StartsWith("[TLO:"))
            {
                string stmp = textToParse.Replace("[TLO:", "").Replace("]", "");
                Position tmppos = new Position(stmp);

                if (!SETTING.pos_TLO.CompareWith(tmppos))
                {
                    SETTING.pos_TLO = tmppos;
                    EventHandler handler = EvStatusMessage;
                    handler?.Invoke(this, new EventVariantStatusMessageArgs(eVariantStatusMessage.posTLO));
                    return;
                }
            }






            bool ChangeFinded = false;
            //TODO: тут и вызовем событие
            // [VER:1.1E.20161219:]
            // [OPT: V]
            


                //////string tmpString = inString.Replace("[", "").Replace("]", "").Trim();

                //////if (tmpString.StartsWith("PRB"))
                //////{
                //////    // это результат сканирования
                //////    string[] arr1 = tmpString.Split(':');

                //////    RealTimeStatus.ProbePinPosition = new Position(arr1[1]);

                //////    //нужно послать уведомление о том что сработал датчик касания
                //////    evProbePinTouch(null);
                //////}

                //////if (tmpString.StartsWith("GC"))
                //////{

                //////    reportParser = new ReportParserState(tmpString);

                //////    //evREPPARSER(null);




                //////}



               // return ChangeFinded;
        }


       

        #endregion

        #region Разное

        /// <summary>
        /// Получение списка возможных ком-портов
        /// </summary>
        /// <returns>Список строк с наименованием портов</returns>
        public List<String> GetListPortName()
        {
            List<string> tmpValue = new List<string>();

            string[] arrayPortNames = SerialPort.GetPortNames();

            foreach (string portName in arrayPortNames)
            {
                tmpValue.Add(portName);
            }

            return tmpValue;
        }


        #endregion
        
        #region РАЗБОР



        #endregion

        /// <summary>
        /// Виды статусов контролера
        /// </summary>
        public enum eVariantStatusMessage
        {
            /// <summary>
            /// Событие что установлена связь
            /// </summary>
            Connected,
            /// <summary>
            /// Событие что связь прекращена
            /// </summary>
            Disconnected,
            /// <summary>
            /// Событие что связь потеряна
            /// </summary>
            LostConnect,
            /// <summary>
            /// Сообщение от ядра контроллера
            /// </summary>
            CoreInfo,
            /// <summary>
            /// Рабочие/пользовательские координаты
            /// </summary>
            mainReport,

            /// <summary>
            /// Координаты установленой системы Gxx
            /// </summary>
            posG54,
            /// <summary>
            /// Координаты установленой системы Gxx
            /// </summary>
            posG55,
            /// <summary>
            /// Координаты установленой системы Gxx
            /// </summary>
            posG56,
            /// <summary>
            /// Координаты установленой системы Gxx
            /// </summary>
            posG57,
            /// <summary>
            /// Координаты установленой системы Gxx
            /// </summary>
            posG58,
            /// <summary>
            /// Координаты установленой системы Gxx
            /// </summary>
            posG59,
            /// <summary>
            /// 
            /// </summary>
            posG28,
            /// <summary>
            /// 
            /// </summary>
            posG30,
            /// <summary>
            /// 
            /// </summary>
            posG92,
            /// <summary>
            /// Координаты срабатывания датчика длины инструмента
            /// </summary>
            posTLO,
            /// <summary>
            /// Координаты срабатывания датчика касания
            /// </summary>
            posPROBE,
            /// <summary>
            /// Получены значения параметров настройки контроллера
            /// </summary>
            Setting







        }

        /// <summary>
        /// Варианты работы с сообщениями
        /// </summary>
        public enum eVariantDataMessage
        {
            /// <summary>
            /// Событие при добавлении РТ команды в стек
            /// </summary>
            CommandRTAdd,
            /// <summary>
            /// Событие при добавлении новой команды в стек
            /// </summary>
            CommandAdd,
            /// <summary>
            /// Событие посылки команды в контроллер
            /// </summary>
            CommandSend,
            /// <summary>
            /// Событие об ответе от контроллера
            /// </summary>
            CommandRecived,
            /// <summary>
            /// Событие об ответе от контроллера
            /// </summary>
            CommandRecivedOther


        }


    } //class Controller
    
    #region Менеджер работы с данными

    /// <summary>
    /// Варианты источников комманд контроллеру
    /// </summary>
    public enum eSourceData
    {
        /// <summary>
        /// Послана пользователем путем ввода произвольной комманды
        /// </summary>
        userCommand,
        /// <summary>
        /// Послана дествием программы при нажатии определённых функций 
        /// </summary>
        appCommand,
        /// <summary>
        /// послана ядром для получения актуальных сведений
        /// </summary>
        refreshInfo,
        /// <summary>
        /// Послано модулем выполнения G-кода
        /// </summary>
        SenderGkode
    }

    /// <summary>
    /// Единичная комманда для посылки
    /// </summary>
    class DataCommand
    {
        /// <summary>
        /// идентификатор строки
        /// </summary>
        public int CommandID;

        /// <summary>
        /// Источник комманды
        /// </summary>
        public eSourceData CommandSource; 
        
        /// <summary>
        /// Сама команда (строка)
        /// </summary>
        public string CommandText;

        /// <summary>
        /// Результат ответа контроллера на посланную команду 
        /// </summary>
        public string ResultCommand;



        /// <summary>
        /// Конструктор команды
        /// </summary>
        public DataCommand(int _CommandID, eSourceData _CommandSource, string _CommandText)
        {
            CommandID = _CommandID;
            CommandSource = _CommandSource;
            CommandText = _CommandText;
            ResultCommand = "";
        }
    }

    /// <summary>
    /// Класс для работы с сообщениями
    /// </summary>
    class DataManager
    {
        /// <summary>
        /// FIFO стек исходящих особо важных сообщений, которые нужно послать
        /// </summary>
        private ConcurrentQueue<byte> StackRealtime;

        /// <summary>
        /// FIFO дополнительный стек исходящих особо важных сообщений, которые нужно послать
        /// </summary>
        private ConcurrentQueue<string> StackRealtimeString;

        /// <summary>
        /// FIFO стек комманд для отправки
        /// </summary>
        private ConcurrentQueue<DataCommand> StackForSend;

        /// <summary>
        /// FIFO стек отправленных комманд
        /// </summary>
        private ConcurrentQueue<DataCommand> StackSending;

        /// <summary>
        /// FIFO стек полученных комманд
        /// </summary>
        private ConcurrentQueue<DataCommand> StackReception;

        /// <summary>
        /// FIFO стек прочих данных принятых от контроллера
        /// </summary>
        private ConcurrentQueue<string> StackOtherData;

        /// <summary>
        /// Уникальный номер для обычных команд
        /// </summary>
        private int IndexRecord;


        /// <summary>
        /// Инициализация классаработы с сообщениями
        /// </summary>
        public DataManager()
        {
            StackRealtime = new ConcurrentQueue<byte>();
            StackRealtimeString = new ConcurrentQueue<string>();
            StackForSend    = new ConcurrentQueue<DataCommand>();
            StackSending    = new ConcurrentQueue<DataCommand>();
            StackReception  = new ConcurrentQueue<DataCommand>();
            StackOtherData       = new ConcurrentQueue<string>();
            IndexRecord = 0;
        }



        #region RT command



        /// <summary>
        /// Добавление RT команды
        /// </summary>
        public void AddRealtimeCommand(byte _val)
        {
            StackRealtime.Enqueue(_val);
        }

        /// <summary>
        /// Добавление RT команды
        /// </summary>
        public void AddRealtimeCommand(string _val)
        {
            StackRealtimeString.Enqueue(_val);
        }



        /// <summary>
        /// Получить внеочередные команды для отправки (из стека данные забирает контроллер)
        /// </summary>
        /// <returns></returns>
        public byte GetRealtimeCommandFromStack()
        {
            byte s = 0;
            StackRealtime.TryDequeue(out s);
            return s;
        }

        /// <summary>
        /// Получить внеочередные команды для отправки (из стека данные забирает контроллер)
        /// </summary>
        /// <returns></returns>
        public string GetRealtimeCommandStringFromStack()
        {
            string s = "";
            StackRealtimeString.TryDequeue(out s);
            return s;
        }

        #endregion


        /// <summary>
        /// Возвращает информацию о наличии первостепенных коммандах для отправки.
        /// </summary>
        /// <returns></returns>
        public bool AvaibleRealTimeCommand
        {
            get => (StackRealtime.Count > 0);
        }

        /// <summary>
        /// Возвращает информацию о наличии первостепенных коммандах для отправки.
        /// </summary>
        /// <returns></returns>
        public bool AvaibleRealTimeCommandString
        {
            get => (StackRealtimeString.Count > 0);
        }


        

        /// <summary>
        /// Наличие комманд для отправки.
        /// </summary>
        /// <returns></returns>
        public bool AvaibleCommand
        {
            get => (StackForSend.Count > 0);
        }

        /// <summary>
        /// Количество комманд в стеке для отправки.
        /// </summary>
        /// <returns></returns>
        public int CountCommand_Send
        {
            get => StackForSend.Count;
        }

        /// <summary>
        /// Количество комманд в стеке посланных в контроллер.
        /// </summary>
        /// <returns></returns>
        public int CountCommand_Sending
        {
            get => StackSending.Count;
        }

        /// <summary>
        /// Количество комманд в стеке которые с ответом.
        /// </summary>
        /// <returns></returns>
        public int CountCommand_Reception
        {
            get => StackReception.Count;
        }


        /// <summary>
        /// Добавление в стек новой команды для отправки
        /// </summary>
        /// <param name="s">строка для добавления</param>
        /// <param name="source">Источник посылки</param>
        /// <returns>Успешно добавлена комманда</returns>
        public void AddSendCommand(string s, eSourceData source)
        {
            StackForSend.Enqueue(new DataCommand(IndexRecord++,source,s));
        }

        /// <summary>
        /// Получение из стека комманды для отправки, но без фиксации отправки данного сообщения
        /// </summary>
        /// <returns>команда для отправки</returns>
        public string GetCommandForSends()
        {
            if (StackForSend.IsEmpty) return "";

            DataCommand dc;

            StackForSend.TryPeek(out dc);

            return dc.CommandText;
        }

        /// <summary>
        /// Получение из стека комманды для отправки, но без фиксации отправки данного сообщения
        /// </summary>
        /// <returns>команда для отправки</returns>
        public DataCommand GetCommandForSend()
        {
            if (StackForSend.IsEmpty) return null;

            DataCommand dc;

            StackForSend.TryPeek(out dc);

            return dc;
        }


        ///// <summary>
        ///// Получение из стека комманды для отправки, но без фиксации отправки данного сообщения
        ///// </summary>
        ///// <returns>команда для отправки</returns>
        //public DataCommand GetCommandReception()
        //{
        //    if (StackReception.IsEmpty) return null;

        //    DataCommand dc;

        //    StackReception.TryPeek(out dc);

        //    return dc;
        //}




        /// <summary>
        /// Получение из стека комманды которую уже отправили, но без удаления из стека
        /// </summary>
        /// <returns>команда для отправки</returns>
        public DataCommand GetCommandSending()
        {
            if (StackSending.IsEmpty) return null;

            DataCommand dc;

            StackSending.TryPeek(out dc);

            return dc;
        }

        /// <summary>
        /// Получение из стека комманды которую уже отправили, с удалением из стека
        /// </summary>
        /// <returns>команда для отправки</returns>
        public DataCommand GetCommandRecived()
        {
            if (StackReception.IsEmpty) return null;

            DataCommand dc;

            //StackReception.TryPeek(out dc);
            StackReception.TryDequeue(out dc);

            return dc;
        }


        


        /// <summary>
        /// Фиксация команды для отправки, что удалось её успешно отправить
        /// </summary>
        public void AcceptCommandForSend()
        {
            if (StackForSend.IsEmpty) return;

            DataCommand dc;

            StackForSend.TryDequeue(out dc); //перемещаем команду в другой стек

            StackSending.Enqueue(dc);
        }

        /// <summary>
        /// Добавление комманды с ответом от контроллера
        /// </summary>
        /// <param name="s">Строка ответа</param>
        public void AddReceiveCommand(string s)
        {
            if (StackSending.IsEmpty)
            {
            // получили сообщение, но его нет в стеке отправленных, какой-то косячок.....
            // добавим в прочий стек
                AddReceiveOtherCommand(s);
            }
            else
            {
                DataCommand dc;
                StackSending.TryDequeue(out dc); //перемещаем команду в другой стек
                dc.ResultCommand = s; //добавив ответ
                StackReception.Enqueue(dc);
            }
        }

        /// <summary>
        /// Добавление комманды с ответом от контроллера
        /// </summary>
        /// <param name="s">Строка ответа</param>
        public void AddReceiveOtherCommand(string s)
        {
            StackOtherData.Enqueue(s);
        }




    }


    /// <summary>
    /// Список комманд для немедленного выполнения
    /// </summary>
    public enum ControllerRealTimeCommand
    {
        /// <summary>
        /// 0x18 - простой сброс
        /// </summary>
        SoftReset = 0x18,

        StatusReportQuery = (byte)'?',

        SafetyDoor         = 0x84,
        /// <summary>
        /// 0х85 - Отмена ручного управления
        /// </summary>
        JogCancel = 0x85,
        /// <summary>
        /// Скорость движения
        /// </summary>
        FeedOverridesSet100 = 0x90,
        /// <summary>
        /// Скорость движения
        /// </summary>
        FeedOverridesIncrease10 = 0x91,
        /// <summary>
        /// Скорость движения
        /// </summary>
        FeedOverridesDecrease10 = 0x92,
        /// <summary>
        /// Скорость движения
        /// </summary>
        FeedOverridesIncrease1 = 0x93,
        /// <summary>
        /// Скорость движения
        /// </summary>
        FeedOverridesDecrease1 = 0x94,

        RapidOverridesSet100            = 0x95,
        RapidOverridesSet50             = 0x96,
        RapidOverridesSet25             = 0x97,
        SpindleSpeedOverridesSet100     = 0x99,
        SpindleSpeedOverridesIncrease10 = 0x9A,
        SpindleSpeedOverridesDecrease10 = 0x9B,
        SpindleSpeedOverridesIncrease1  = 0x9C,
        SpindleSpeedOverridesDecrease1  = 0x9D,
        ToggleSpindleStop               = 0x9E,
        ToggleFloodCoolant              = 0xA0,
        ToggleMistCoolant               = 0xA1,
        StartResume   = (byte)'~',
        Hold          = (byte)'!'
    }


    public enum ControllerCommand
    {
        /// <summary>
        /// Запрос текущих координат, у систем координат G54,55,56,57,58,59 а так-же G28,G30 и Координаты датчика длины инструмента TLO,
        /// и координат касания PRB  
        /// View gcode parameters - $#
        /// </summary>
        query_POS,

        /// <summary>
        /// Сброс аварийного состояния "$X"
        /// </summary>
        KillAlarmLock,

        /// <summary>
        /// "$RST=$" - Сброс пользовательских настроек
        /// </summary>
        Reset_ControllerSetting,

        /// <summary>
        /// "$RST=#" - Сброс координат G54,G55,G56,G57,G58,G59
        /// </summary>
        Reset_Coordinates,

        /// <summary>
        /// "$RST=*" - полный сброс всех параметов
        /// </summary>
        Reset_All,

        /// <summary>
        /// "$SLP" - перевод контроллера в спящий режим
        /// </summary>
        SLEEP,

        /// <summary>
        /// "$C" - вкл/выключение тестового режима
        /// </summary>
        TestMode,

        /// <summary>
        /// "$I" - информация о прошивке
        /// </summary>
        AboutFirmware,


        /// <summary>
        /// "$H" - Запуск процедуры поиска дома
        /// </summary>
        ToHome,

        /// <summary>
        /// "$G" - Запрос параметров парсинга данных
        /// </summary>
        query_Parsing,

        /// <summary>
        /// $$ - запрос параметров
        /// </summary>
        query_setting


    }


    #endregion

    #region Параметры контроллера





    /// <summary>
    /// Setting the controller
    /// </summary>
    public class Setting
    {
        public string PortName; // portname as "COM1"
        public int PortSpeed;   
        public bool ResetControllerInConnect;
        /// <summary>
        /// Количество осей у контроллера
        /// </summary>
        public AxisVariant CountAxes;



        public Position pos_Machine;
        public Position pos_Works;
        public Position pos_WCO;
        public Position pos_ProbePin;
        public string pos_MAIN;


        public Position pos_G54;
        public Position pos_G55;
        public Position pos_G56;
        public Position pos_G57;
        public Position pos_G58;
        public Position pos_G59;
        public Position pos_G28;
        public Position pos_G30;
        public Position pos_G92;
        public Position pos_TLO;


        /// <summary>
        /// Количество строк, которое можно послать в контроллер
        /// </summary>
        public int buffer_Size;
        public int buffer_RX;

        public int Speed;
        public int Power;

        public int OverrideFeed;
        public int OverrideRapid;
        public int OverridePower;

        /// <summary>
        /// Интервал между запросами, статуса у контроллера
        /// может иметь значение от нуля до любого
        /// </summary>
        public int IntervalRefreshPos;

        /// <summary>
        /// Интервал между запросами, статуса у контроллера
        /// может иметь значение от нуля до 1000
        /// </summary>
        public int IntervalRefreshStatus;

        public int p00_StepPulse;               // microseconds 1-255
        public int p01_StepIdleDelay;           // milliseconds 0-255
        public int p02_StepPortInvertMask;      // mask
        public int p03_DirectionPortInvertMask; // mask
        public int p04_StepEnableInvert;        // bool
        public int p05_LimitPinsInvert;         // bool
        public int p06_ProbePinInvert;          // bool
        public int p10_StatusReport;            // mask
        public decimal p11_JunctionDeviation;   // mm
        public decimal p12_ArcTolerance;        // mm
        public int p13_ReportInches;            // bool
        public int p20_SoftLimit;               // bool
        public int p21_HardLimit;               // bool
        public int p22_HomingCycle;             // bool
        public int p23_HomingDirInvert;         // mask
        public decimal p24_HomingFeed;          // mm/min
        public decimal p25_HomingSeek;          // mm/min
        public int p26_HomingDebounce;          // microseconds
        public decimal p27_HomingPullOff;       // mm
        public int p30_MaxSpindleSpeed;         // RPM
        public int p31_MinSpindleSpeed;         // RPM
        public int p32_LaserMode;               // bool

        public decimal p100_StepX;              // steps/mm
        public decimal p101_StepY;              // steps/mm
        public decimal p102_StepZ;              // steps/mm
        public decimal p103_StepA;              // steps/mm
        public decimal p104_StepB;              // steps/mm

        public decimal p110_MaxRateX;           // mm/min
        public decimal p111_MaxRateY;           // mm/min
        public decimal p112_MaxRateZ;           // mm/min
        public decimal p113_MaxRateA;           // mm/min
        public decimal p114_MaxRateB;           // mm/min

        public decimal p120_AccelerationX;      // mm/sec^2
        public decimal p121_AccelerationY;      // mm/sec^2
        public decimal p122_AccelerationZ;      // mm/sec^2
        public decimal p123_AccelerationA;      // mm/sec^2
        public decimal p124_AccelerationB;      // mm/sec^2

        public decimal p130_MaxTravelX;         // mm
        public decimal p131_MaxTravelY;         // mm
        public decimal p132_MaxTravelZ;         // mm
        public decimal p133_MaxTravelA;         // mm
        public decimal p134_MaxTravelB;         // mm

        //11 и 12 дробный параметр


        //13,20,21,22,23 - целый

        //24,25 - дробный

        //26 целый

        //27 - дробный

        //30 - целый

        // 31,32 - целый

        //100,101,102,103,110,111,112,113,120,121,122,123,130,131,132,133 - дробные


        public CurentModeParameters Gcode_ParserState;
    }

    /// <summary>
    /// Варианты использования осей у контроллера
    /// </summary>
    public enum AxisVariant
    {
        XYZ,
        XYZA,
        XYZAB
    }


    /// <summary>
    /// Варианты состояния контроллера
    /// </summary>
    public enum EnumStatusDevice
    {
        /// <summary>
        /// Выключен
        /// </summary>
        Off = 0,
        /// <summary>
        /// Ожидание команд
        /// </summary>
        Idle = 1,
        /// <summary>
        /// Выполняется работа
        /// </summary>
        Run = 2,
        /// <summary>
        /// Пауза
        /// </summary>
        Hold = 3,
        /// <summary>
        /// Открыта дверца
        /// </summary>
        Door = 4,
        /// <summary>
        /// Поиск дома
        /// </summary>
        Home = 5,
        /// <summary>
        /// Авария
        /// </summary>
        Alarm = 6,
        /// <summary>
        /// Режим тестирования
        /// </summary>
        Check = 7,
        /// <summary>
        /// Режим сна
        /// </summary>
        Sleep = 8,
        /// <summary>
        /// Режим ручного перемещения
        /// </summary>
        JOG = 9
    }

    /// <summary>
    /// Координаты
    /// </summary>
    public class Position
    {
        private int _countAxes;

        //todo: продумать как использовать оси

        private Axle[] _pos;


        public decimal X;
        public decimal Y;
        public decimal Z;
        public decimal A;
        public decimal B;

        public Position(decimal _X, decimal _Y, decimal _Z, decimal _A=0, decimal _B=0)
        {
            X = _X;
            Y = _Y;
            Z = _Z;
            A = _A;
            B = _B;
        }

        public Position(string str)
        {
            //конструктор на основании строки  0.000,0.000,0.000:1

            X = 0;
            Y = 0;
            Z = 0;
            A = 0;
            B = 0;

            string[] sarr = str.Split(',');

            if (sarr.Length == 3)
            {
                decimal.TryParse(sarr[0].Replace('.', ','), out X);
                decimal.TryParse(sarr[1].Replace('.', ','), out Y);
                decimal.TryParse(sarr[2].Replace('.', ','), out Z);
            }

            if (sarr.Length == 4)
            {
                decimal.TryParse(sarr[0].Replace('.', ','), out X);
                decimal.TryParse(sarr[1].Replace('.', ','), out Y);
                decimal.TryParse(sarr[2].Replace('.', ','), out Z);
                decimal.TryParse(sarr[3].Replace('.', ','), out A);
            }

            if (sarr.Length == 5)
            {
                decimal.TryParse(sarr[0].Replace('.', ','), out X);
                decimal.TryParse(sarr[1].Replace('.', ','), out Y);
                decimal.TryParse(sarr[2].Replace('.', ','), out Z);
                decimal.TryParse(sarr[3].Replace('.', ','), out A);
                decimal.TryParse(sarr[4].Replace('.', ','), out B);
            }
        }

        public override string ToString()
        {
            return X.ToString("#0.000") + " : " + Y.ToString("#0.000") + " : " + Z.ToString("#0.000");
        }

        /// <summary>
        /// Сравнение текущих координат с указанными
        /// </summary>
        /// <param name="pos">Координаты с которыми сравнить</param>
        /// <returns>Истина - Координаты совпадают</returns>
        public bool CompareWith(Position pos)
        {
            return (X == pos.X && Y == pos.Y && Z == pos.Z && A == pos.A && B == pos.B);
        }

    }



    

    /// <summary>
    /// Параметры установленных режимов контроллера
    /// </summary>
    public class CurentModeParameters
    {
        public readonly eMotionMode               MotionMode;
        public readonly eCoordinateSystem         CoordinateSystem;
        public readonly ePlaneSelect              PlaneSelect;
        public readonly eDistanceMode             DistanceMode;
        public readonly eArcIJKDistanceMode       ArcIJKDistanceMode;
        public readonly eFeedRateMode             FeedRateMode;
        public readonly eUnitMode                 UnitMode;
        public readonly eCutterRadiusCompensation CutterRadiusCompensatio;
        public readonly eToolLenghtOffset         ToolLenghtOffset;
        public readonly eProgramMode              ProgramMode;
        public readonly eSpindleState             SpindleState;
        public readonly eCoolantState             CoolantState;
        public readonly int ToolsNumber;
        public readonly long Svalue;
        public readonly long Fvalue;

        private string decimalSeparator;
      
        /// <summary>
        /// Конструктор который устанавливает дфолные настройки
        /// которые идеинтичны тем что в прошивке
        /// </summary>
        public CurentModeParameters()
        {
            MotionMode              = eMotionMode.G0;
            CoordinateSystem        = eCoordinateSystem.G54;
            PlaneSelect             = ePlaneSelect.G17;
            UnitMode                = eUnitMode.G21;
            DistanceMode            = eDistanceMode.G90;
            FeedRateMode            = eFeedRateMode.G94;
            SpindleState            = eSpindleState.M5;
            CoolantState            = eCoolantState.M9;
            ToolsNumber = 0;
            Svalue      = 0;
            Fvalue      = 0;
            ArcIJKDistanceMode      = eArcIJKDistanceMode.G91_1;
            CutterRadiusCompensatio = eCutterRadiusCompensation.G40;
            ToolLenghtOffset        = eToolLenghtOffset.G49;
            ProgramMode             = eProgramMode.M0;
            decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        }


        // образец: [GC: G0 G54 G17 G21 G90 G94    M5 M9 T0 F0   S0] 
        // или      [GC: G0 G54 G17 G21 G90 G94 M0 M5 M9 T0 S0.0 F500.0]

        public CurentModeParameters(string svalue)
        {
            MotionMode = eMotionMode.G0;
            CoordinateSystem = eCoordinateSystem.G54;
            PlaneSelect = ePlaneSelect.G17;
            UnitMode = eUnitMode.G21;
            DistanceMode = eDistanceMode.G90;
            FeedRateMode = eFeedRateMode.G94;
            SpindleState = eSpindleState.M5;
            CoolantState = eCoolantState.M9;
            ToolsNumber = 0;
            Svalue = 0;
            Fvalue = 0;
            ArcIJKDistanceMode = eArcIJKDistanceMode.G91_1;
            CutterRadiusCompensatio = eCutterRadiusCompensation.G40;
            ToolLenghtOffset = eToolLenghtOffset.G49;
            ProgramMode = eProgramMode.M0;
            decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;


            string tmps = svalue.Replace("[GC:", "").Replace("]", "").Trim().ToUpper();

            string[] arrStr = tmps.Split(' ');

            // а теперь заполним
            foreach (string s in arrStr)
            {
                if (s == "G0")    MotionMode = eMotionMode.G0;
                if (s == "G1")    MotionMode = eMotionMode.G1;
                if (s == "G2")    MotionMode = eMotionMode.G2;
                if (s == "G3")    MotionMode = eMotionMode.G3;
                if (s == "G38.2") MotionMode = eMotionMode.G38_2;
                if (s == "G38.3") MotionMode = eMotionMode.G38_3;
                if (s == "G38.4") MotionMode = eMotionMode.G38_4;
                if (s == "G38.5") MotionMode = eMotionMode.G38_5;
                if (s == "G80")   MotionMode = eMotionMode.G80;

                if (s == "G54") CoordinateSystem = eCoordinateSystem.G54;
                if (s == "G55") CoordinateSystem = eCoordinateSystem.G55;
                if (s == "G56") CoordinateSystem = eCoordinateSystem.G56;
                if (s == "G57") CoordinateSystem = eCoordinateSystem.G57;
                if (s == "G58") CoordinateSystem = eCoordinateSystem.G58;
                if (s == "G59") CoordinateSystem = eCoordinateSystem.G59;

                if (s == "G17") PlaneSelect = ePlaneSelect.G17;
                if (s == "G18") PlaneSelect = ePlaneSelect.G18;
                if (s == "G19") PlaneSelect = ePlaneSelect.G19;

                if (s == "G20") UnitMode = eUnitMode.G20;
                if (s == "G21") UnitMode = eUnitMode.G21;

                if (s == "G90") DistanceMode = eDistanceMode.G90;
                if (s == "G91") DistanceMode = eDistanceMode.G91;

                if (s == "G93") FeedRateMode = eFeedRateMode.G93;
                if (s == "G94") FeedRateMode = eFeedRateMode.G94;

                if (s == "M3") SpindleState = eSpindleState.M3;
                if (s == "M4") SpindleState = eSpindleState.M4;
                if (s == "M5") SpindleState = eSpindleState.M5;

                if (s == "M7") CoolantState = eCoolantState.M7;
                if (s == "M8") CoolantState = eCoolantState.M8;
                if (s == "M9") CoolantState = eCoolantState.M9;

                if (s == "M0" ) ProgramMode = eProgramMode.M0;
                if (s == "M1" ) ProgramMode = eProgramMode.M1;
                if (s == "M2" ) ProgramMode = eProgramMode.M2;
                if (s == "M30") ProgramMode = eProgramMode.M30;

                if (s.StartsWith("S"))
                {
                    Svalue = GetLongFromString(s.Substring(1));
                }

                if (s.StartsWith("F"))
                {
                    Fvalue = GetLongFromString(s.Substring(1));
                }

                if (s.StartsWith("T"))
                {
                    ToolsNumber = GetIntFromString(s.Substring(1));
                }
            }
        }


        /// <summary>
        /// Проверка идеинтичности данных
        /// </summary>
        /// <param name="_sp"></param>
        /// <returns></returns>
        public bool Compare(CurentModeParameters _sp)
        {
            if (MotionMode       != _sp.MotionMode)       return false;
            if (CoordinateSystem != _sp.CoordinateSystem) return false;
            if (PlaneSelect      != _sp.PlaneSelect)  return false;
            if (UnitMode         != _sp.UnitMode)     return false;
            if (DistanceMode     != _sp.DistanceMode) return false;
            if (FeedRateMode     != _sp.FeedRateMode) return false;
            if (SpindleState     != _sp.SpindleState) return false;
            if (CoolantState     != _sp.CoolantState) return false;
            if (ToolsNumber      != _sp.ToolsNumber)  return false;
            if (Svalue           != _sp.Svalue)       return false;
            if (Fvalue           != _sp.Fvalue)       return false;
            if (ArcIJKDistanceMode      != _sp.ArcIJKDistanceMode)      return false;
            if (CutterRadiusCompensatio != _sp.CutterRadiusCompensatio) return false;
            if (ToolLenghtOffset        != _sp.ToolLenghtOffset)        return false;
            if (ProgramMode             != _sp.ProgramMode)             return false;

            return true;
        }

        private long GetLongFromString(string s)
        {
            long tmpValue = 0;

            if (decimalSeparator == ".") long.TryParse(s.Replace(",", "."), out tmpValue);

            if (decimalSeparator == ",") long.TryParse(s.Replace(".", ","), out tmpValue);

            return tmpValue;

        }

        private int GetIntFromString(string s)
        {
            int tmpValue = 0;

            int.TryParse(s.Replace(",", "."), out tmpValue);

            return tmpValue;

        }

        public enum eMotionMode
        {
            G0,
            G1,
            G2,
            G3,
            G38_2,//probe pin
            G38_3,//probe pin
            G38_4,//probe pin
            G38_5,//probe pin
            G80   // Cancel Canned Cycle 
        }

        public enum eCoordinateSystem
        {
            G54,
            G55,
            G56,
            G57,
            G58,
            G59
        }

        public enum ePlaneSelect
        {
            G17,//XY
            G18,//ZX
            G19 //YZ
        }

        public enum eDistanceMode
        {
            G90,//абсолютная система пололожения
            G91  //относительная
        }

        public enum eFeedRateMode
        {
            G93,
            G94
        }

        public enum eSpindleState
        {
            M3, //включение по часовой
            M4, //включение против часовой
            M5  //выключение
        }

        public enum eCoolantState
        {
            M7,
            M8,
            M9
        }
        
        public enum eArcIJKDistanceMode
        {
            G91_1 //относительная для дуг
        }
        
        public enum eUnitMode
        {
            G20, //дюймы
            G21  //милиметры
        }

        public enum eCutterRadiusCompensation
        {
            G40 //выключение компенсации
        }

        public enum eToolLenghtOffset
        {
            G43_1, // что-то про компенсацию учет длины инструмента
            G49    // отмена компенсации длины
        }

        public enum eProgramMode
        {
            M0, //пауза
            M1, //пауза
            M2, //завершение программы
            M30 //завершение программы
        }
    }
   



    #endregion

    #region прочее








      /// <summary>
    /// Список контроллеров, которыми может управлять данный модуль
    /// </summary>
    public enum DeviceBoard
    {
        /// <summary>
        /// Использовать STM32 микроконтроллер
        /// </summary>
        STM32F106C8T6 = 0,
        /// <summary>
        /// Использовать ардуино микроконтроллер
        /// </summary>
        ATMEGA328 = 1,     //arduino
    }

     /// <summary>
    /// Класс одной оси
    /// </summary>
    class Axle
    {
        /// <summary>
        /// Номер оси
        /// </summary>
        private int _AxisNumber;
        /// <summary>
        /// Наименование оси
        /// </summary>
        private string _AxisName;
        /// <summary>
        /// Текущее значение координаты данной оси
        /// </summary>
        private decimal _AxisPosition;

        /// <summary>
        /// Инициализация оси
        /// </summary>
        /// <param name="AxisNumber">Номер оси</param>
        /// <param name="AxisName">Наименование оси</param>
        /// <param name="AxisPosition">Текущаяя координата оси</param>
        public Axle(int AxisNumber, string AxisName, decimal AxisPosition = 0)
        {
            _AxisNumber = AxisNumber;
            _AxisName = AxisName;
            _AxisPosition = AxisPosition;
        }


        /// <summary>
        /// Номер оси
        /// </summary>
        public int AxisNumber
        {
            get { return _AxisNumber; }
        }

        /// <summary>
        /// Наименование оси
        /// </summary>
        public string AxisName
        {
            get { return _AxisName; }
        }

        /// <summary>
        /// Текущее значение координаты данной оси
        /// </summary>
        public decimal AxisPosition
        {
            get { return _AxisPosition; }
            set { _AxisPosition = value; }
        }


    }
  

    #endregion
}
