using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace CoreControllers
{
    public partial class TestCoreGRBL : Form
    {
        /// <summary>
        /// Модуль для работы GRBL
        /// </summary>
        private Controller GRBL;

        #region Запуск/закрытие формы

        public TestCoreGRBL()
        {
            InitializeComponent();
        }




        

        private void Form1_Load(object sender, EventArgs e)
        {
            Text = @"Тестирование GRBL (версия " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion + ")";


            // скроем некоторые уведомления
            labelLostConnect.Visible = false;
            labelErrorConnect.Visible = false;


            GRBL = new Controller();
            // подключение подписок 
            GRBL.EvStatusMessage += GRBL_EvStatusMessage;
            GRBL.EvDataMessage   += GRBL_EvDataMessage;
            GRBL.EvRefreshCurentModeParameters += GRBL_EvRefreshCurentModeParameters; //событие при изменении режимов контроллера
            GRBL.evInfo            += GRBL_evInfo;                                    // Информационное сообщение от библиотеки работающей с контроллером
            GRBL.EvTouchPin        += GRBL_EvTouchPin;                                // Сработал датчик касания
            // установим частоту обновления данных 10 раз в секунду
            numTimeRefresh.Value = GRBL.SETTING.IntervalRefreshPos;
            numTimeRefreshStatus.Value = GRBL.SETTING.IntervalRefreshStatus;

            RefreshListAvablePorts();





 
        }



        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Остановим обновление данных
            GRBL.SETTING.IntervalRefreshPos = 0;
            //отменим подписку
            GRBL.EvStatusMessage -= GRBL_EvStatusMessage;
            GRBL.EvDataMessage   -= GRBL_EvDataMessage;
            GRBL.EvRefreshCurentModeParameters -= GRBL_EvRefreshCurentModeParameters;
            GRBL.evInfo            -= GRBL_evInfo;
            GRBL.EvTouchPin        -= GRBL_EvTouchPin;

            // и лишь потом инициируем отключение, т.к. форма закрывается раньше
            // чем от GRBL прилетит сообщение от подписки (для формы), то подписка вызовет исключение, 
            // в связи с тем что форма уже не существует....
            if (GRBL.IsConnected) GRBL.Disconect();

        }

        #endregion

        #region События от контроллера

        private void GRBL_EvStatusMessage(object sender, EventArgs e)
        {
            // что-бы понимать кикие именно данные получены от контроллера
            Controller.eVariantStatusMessage cev = ((Controller.EventVariantStatusMessageArgs)e).Value;

            try
            {
                //TODO: При нажатии на крестик программы программа завершается раньше чем события отключаются
                switch (cev)
                {
                    case Controller.eVariantStatusMessage.Connected:
                        this.Invoke(new EventHandler(SetConnectStatus));
                        break;
                    case Controller.eVariantStatusMessage.Disconnected:
                        this.Invoke(new EventHandler(SetDisconnectStatus));
                        break;
                    case Controller.eVariantStatusMessage.LostConnect:
                        this.Invoke(new EventHandler(SetLostConnectStatus));
                        break;
                    case Controller.eVariantStatusMessage.Setting:
                        this.Invoke(new EventHandler(RefreshProperty));
                        break;
                    case Controller.eVariantStatusMessage.mainReport:
                        this.Invoke(new EventHandler(REfreshDeviceInfo));
                        break;
                    case Controller.eVariantStatusMessage.posG54:
                        this.Invoke(new EventHandler(REfreshPosG54));
                        break;

                    case Controller.eVariantStatusMessage.posG55:
                        this.Invoke(new EventHandler(REfreshPosG55));
                        break;

                    case Controller.eVariantStatusMessage.posG56:
                        this.Invoke(new EventHandler(REfreshPosG56));
                        break;

                    case Controller.eVariantStatusMessage.posG57:
                        this.Invoke(new EventHandler(REfreshPosG57));
                        break;

                    case Controller.eVariantStatusMessage.posG58:
                        this.Invoke(new EventHandler(REfreshPosG58));
                        break;

                    case Controller.eVariantStatusMessage.posG59:
                        this.Invoke(new EventHandler(REfreshPosG59));
                        break;

                    case Controller.eVariantStatusMessage.posG28:
                        this.Invoke(new EventHandler(REfreshPosG28));
                        break;

                    case Controller.eVariantStatusMessage.posG30:
                        this.Invoke(new EventHandler(REfreshPosG30));
                        break;

                    case Controller.eVariantStatusMessage.posG92:
                        this.Invoke(new EventHandler(REfreshPosG92));
                        break;

                    case Controller.eVariantStatusMessage.posTLO:
                        this.Invoke(new EventHandler(REfreshPosTLO));
                        break;
                }

            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }


        }

        private void SetConnectStatus(object sender, EventArgs e)
        {
            groupBoxSendText.Enabled = true;
            groupBoxSettingConnect.Enabled = false;
            btConnectDisconnect.Text = @"Отключиться от контроллера";
            labelErrorConnect.Visible = false;
            refreshMP();
        }  

        private void SetDisconnectStatus(object sender, EventArgs e)
        {
            REfreshDeviceInfo(this, new EventArgs());
            groupBoxSendText.Enabled = false;
            groupBoxSettingConnect.Enabled = true;
            btConnectDisconnect.Text = @"Подключиться к контроллеру";
        }

        private void SetLostConnectStatus(object sender, EventArgs e)
        {
            REfreshDeviceInfo(this,new EventArgs());
            groupBoxSendText.Enabled = false;
            groupBoxSettingConnect.Enabled = true;
            btConnectDisconnect.Text = @"Подключиться к контроллеру";
            labelLostConnect.Visible = true;
        }

        private void RefreshProperty(object sender, EventArgs e)
        {
            p00_StepPulse.Value = GRBL.SETTING.p00_StepPulse;
            p01_StepIdleDelay.Value = GRBL.SETTING.p01_StepIdleDelay;
            p02_StepPortInvertMask.SetNewValue(GRBL.SETTING.p02_StepPortInvertMask);
            p03_DirectionPortInvertMask.SetNewValue(GRBL.SETTING.p03_DirectionPortInvertMask);

            p04_StepEnableInvert.Checked = (GRBL.SETTING.p04_StepEnableInvert == 1);
            p05_LimitPinsInvert.Checked = (GRBL.SETTING.p05_LimitPinsInvert == 1);
            p06_ProbePinInvert.Checked = (GRBL.SETTING.p06_ProbePinInvert == 1);


            p11_JunctionDeviation.Value = GRBL.SETTING.p11_JunctionDeviation;
            p12_ArcTolerance.Value = GRBL.SETTING.p12_ArcTolerance;

            p13_ReportInches.Checked = (GRBL.SETTING.p13_ReportInches == 1);
            p20_SoftLimit.Checked = (GRBL.SETTING.p20_SoftLimit == 1);
            p21_HardLimit.Checked = (GRBL.SETTING.p21_HardLimit == 1);

            p32_LaserMode.Checked = (GRBL.SETTING.p32_LaserMode == 1);

            p100_StepX.Value = GRBL.SETTING.p100_StepX;
            p101_StepY.Value = GRBL.SETTING.p101_StepY;
            p102_StepZ.Value = GRBL.SETTING.p102_StepZ;
            p103_StepA.Value = GRBL.SETTING.p103_StepA;
            p104_StepB.Value = GRBL.SETTING.p104_StepB;

            p110_MaxRateX.Value = GRBL.SETTING.p110_MaxRateX;
            p111_MaxRateY.Value = GRBL.SETTING.p111_MaxRateY;
            p112_MaxRateZ.Value = GRBL.SETTING.p112_MaxRateZ;
            p113_MaxRateA.Value = GRBL.SETTING.p113_MaxRateA;
            p114_MaxRateB.Value = GRBL.SETTING.p114_MaxRateB;

            p120_AccelerationX.Value = GRBL.SETTING.p120_AccelerationX;
            p121_AccelerationY.Value = GRBL.SETTING.p121_AccelerationY;
            p122_AccelerationZ.Value = GRBL.SETTING.p122_AccelerationZ;
            p123_AccelerationA.Value = GRBL.SETTING.p123_AccelerationA;
            p124_AccelerationB.Value = GRBL.SETTING.p124_AccelerationB;

            p130_MaxTravelX.Value = GRBL.SETTING.p130_MaxTravelX;
            p131_MaxTravelY.Value = GRBL.SETTING.p131_MaxTravelY;
            p132_MaxTravelZ.Value = GRBL.SETTING.p132_MaxTravelZ;
            p133_MaxTravelA.Value = GRBL.SETTING.p133_MaxTravelA;
            p134_MaxTravelB.Value = GRBL.SETTING.p134_MaxTravelB;

        }




        private void REfreshDeviceInfo(object sender, EventArgs e)
        {
            tb_pos_m_X.Text = GRBL.SETTING.pos_Machine.X.ToString("#0.000");
            tb_pos_m_Y.Text = GRBL.SETTING.pos_Machine.Y.ToString("#0.000");
            tb_pos_m_Z.Text = GRBL.SETTING.pos_Machine.Z.ToString("#0.000");
            tb_pos_m_A.Text = GRBL.SETTING.pos_Machine.A.ToString("#0.000");
            tb_pos_m_B.Text = GRBL.SETTING.pos_Machine.B.ToString("#0.000");

            tb_pos_w_X.Text = GRBL.SETTING.pos_Works.X.ToString("#0.000");
            tb_pos_w_Y.Text = GRBL.SETTING.pos_Works.Y.ToString("#0.000");
            tb_pos_w_Z.Text = GRBL.SETTING.pos_Works.Z.ToString("#0.000");
            tb_pos_w_A.Text = GRBL.SETTING.pos_Works.A.ToString("#0.000");
            tb_pos_w_B.Text = GRBL.SETTING.pos_Works.B.ToString("#0.000");


            if (GRBL.SETTING.CountAxes == AxisVariant.XYZ)
            {
                tb_pos_w_A.Enabled = false;
                tb_pos_m_A.Enabled = false;
                tb_pos_w_B.Enabled = false;
                tb_pos_m_B.Enabled = false;

                p103_StepA.Enabled = false;
                p113_MaxRateA.Enabled = false;
                p123_AccelerationA.Enabled = false;
                p133_MaxTravelA.Enabled = false;
                p104_StepB.Enabled = false;
                p114_MaxRateB.Enabled = false;
                p124_AccelerationB.Enabled = false;
                p134_MaxTravelB.Enabled = false;
            }

            if (GRBL.SETTING.CountAxes == AxisVariant.XYZA)
            {
                tb_pos_w_A.Enabled = true;
                tb_pos_m_A.Enabled = true;
                tb_pos_w_B.Enabled = false;
                tb_pos_m_B.Enabled = false;

                p103_StepA.Enabled = true;
                p113_MaxRateA.Enabled = true;
                p123_AccelerationA.Enabled = true;
                p133_MaxTravelA.Enabled = true;
                p104_StepB.Enabled = false;
                p114_MaxRateB.Enabled = false;
                p124_AccelerationB.Enabled = false;
                p134_MaxTravelB.Enabled = false;
            }

            if (GRBL.SETTING.CountAxes == AxisVariant.XYZAB)
            {
                tb_pos_w_A.Enabled = true;
                tb_pos_m_A.Enabled = true;
                tb_pos_w_B.Enabled = true;
                tb_pos_m_B.Enabled = true;

                p103_StepA.Enabled = true;
                p113_MaxRateA.Enabled = true;
                p123_AccelerationA.Enabled = true;
                p133_MaxTravelA.Enabled = true;
                p104_StepB.Enabled = true;
                p114_MaxRateB.Enabled = true;
                p124_AccelerationB.Enabled = true;
                p134_MaxTravelB.Enabled = true;
            }

            switch (GRBL.STATUS)
            {
                case EnumStatusDevice.Idle:  textBoxStatus.Text = @"Готов";        break;
                case EnumStatusDevice.Run:   textBoxStatus.Text = @"Выполняется";  break;
                case EnumStatusDevice.Hold:  textBoxStatus.Text = @"Пауза";        break;
                case EnumStatusDevice.Door:  textBoxStatus.Text = @"Дверь";        break;
                case EnumStatusDevice.Home:  textBoxStatus.Text = @"Поиск начала"; break;
                case EnumStatusDevice.Alarm: textBoxStatus.Text = @"Авария";       break;
                case EnumStatusDevice.Check: textBoxStatus.Text = @"+ТЕСТ+";       break;
                case EnumStatusDevice.Sleep: textBoxStatus.Text = @"Режим сна";    break;
                case EnumStatusDevice.JOG  : textBoxStatus.Text = @"Ручн. перемещ."; break;
                case EnumStatusDevice.Off:   textBoxStatus.Text = @"НЕТ СВЯЗИ";    break;
                default: break;
            }

            if (GRBL.SETTING.buffer_Size > progressBarBufferSize.Maximum)
            {
                progressBarBufferSize.Maximum = GRBL.SETTING.buffer_Size;

            }

            progressBarBufferSize.Value = GRBL.SETTING.buffer_Size;



            lbbuffSize.Text = GRBL.SETTING.buffer_Size.ToString();

            textBoxSpeed.Text = GRBL.SETTING.Speed.ToString();
            tbOverspeed.Text = GRBL.SETTING.OverrideFeed.ToString();
            tbOverPower.Text = GRBL.SETTING.OverridePower.ToString();          
        }

        private void REfreshPosG54(object sender, EventArgs e)
        {
            parserStatus_g54.Text = GRBL.SETTING.pos_G54.ToString();
        }
        private void REfreshPosG55(object sender, EventArgs e)
        {
            parserStatus_g55.Text = GRBL.SETTING.pos_G55.ToString();
        }
        private void REfreshPosG56(object sender, EventArgs e)
        {
            parserStatus_g56.Text = GRBL.SETTING.pos_G56.ToString();
        }
        private void REfreshPosG57(object sender, EventArgs e)
        {
            parserStatus_g57.Text = GRBL.SETTING.pos_G57.ToString();
        }
        private void REfreshPosG58(object sender, EventArgs e)
        {
            parserStatus_g58.Text = GRBL.SETTING.pos_G58.ToString();
        }
        private void REfreshPosG59(object sender, EventArgs e)
        {
            parserStatus_g59.Text = GRBL.SETTING.pos_G59.ToString();
        }
        private void REfreshPosG28(object sender, EventArgs e)
        {
            parserStatus_g28.Text = GRBL.SETTING.pos_G28.ToString();
        }
        private void REfreshPosG30(object sender, EventArgs e)
        {
            parserStatus_g30.Text = GRBL.SETTING.pos_G30.ToString();
        }
        private void REfreshPosG92(object sender, EventArgs e)
        {
            parserStatus_g92.Text = GRBL.SETTING.pos_G92.ToString();
        }
        private void REfreshPosTLO(object sender, EventArgs e)
        {
            parserStatus_TLO.Text = GRBL.SETTING.pos_TLO.ToString();
        }



        //---------------------------

        private void GRBL_EvDataMessage(object sender, EventArgs e)
        {
            // что-бы понимать кикие именно данные получены от контроллера
            Controller.eVariantDataMessage cev = ((Controller.EventСmdArgs)e).variant;

            switch (cev)
            {
                case Controller.eVariantDataMessage.CommandRTAdd:
                    TableAddRTCommand(((Controller.EventСmdArgs)e).RTvalue);
                    break;
                case Controller.eVariantDataMessage.CommandAdd:
                    TableAddCommand(((Controller.EventСmdArgs)e).Value);
                    break;
                case Controller.eVariantDataMessage.CommandSend:
                    TableSendCommand(((Controller.EventСmdArgs)e).Value);
                    break;
                case Controller.eVariantDataMessage.CommandRecived:
                    TableRecivedCommand(((Controller.EventСmdArgs)e).Value);
                    break;
                case Controller.eVariantDataMessage.CommandRecivedOther:
                    TableAddOther(((Controller.EventСmdArgs)e).strValue);
                    break;
            }
        }

        private void TableAddRTCommand(byte value)
        {
            if (!cbRTcommand.Checked) return; //не нужно выводить данное сообщение

            if (InvokeRequired)
            {
                this.Invoke(new Action<byte>(TableAddRTCommand), new object[] { value });
                return;
            }

            int pos = dataGridViewMessages.Rows.Add();
            dataGridViewMessages.Rows[pos].Cells["colDir"].Value = imageList1.Images[3];
            dataGridViewMessages.Rows[pos].Cells["colStatus"].Value = imageList1.Images[2];
            dataGridViewMessages.Rows[pos].Cells["colID"].Value = "";
            dataGridViewMessages.Rows[pos].Cells["colTextSend"].Value = value.ToString("X");

            if (cbScroll.Checked) dataGridViewMessages.FirstDisplayedScrollingRowIndex = dataGridViewMessages.RowCount - 1;
        }

        private void TableAddCommand(DataCommand value)
        {
            // проверим необходимость вывода сообщений
            if (value.CommandSource == eSourceData.refreshInfo && !cbquerty.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.appCommand && !cbGUI.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.userCommand && !cbUserCommand.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.SenderGkode && !cbGcodeSender.Checked) return; //не нужно выводить данное сообщение

            if (InvokeRequired)
            {
                this.Invoke(new Action<DataCommand>(TableAddCommand), new object[] { value });
                return;
            }


            int rowIndexFind = -1;
            string sFind = value.CommandID.ToString();

            for (int indxRow = 0; indxRow < dataGridViewMessages.Rows.Count; indxRow++)
            {
                if (dataGridViewMessages.Rows[indxRow].Cells["colID"].Value.ToString() == sFind)
                {
                    rowIndexFind = indxRow;
                    break;
                }
            }

            if (rowIndexFind != -1) return; //TODO: пока так избавимся от дублей


            int pos = dataGridViewMessages.Rows.Add();
            dataGridViewMessages.Rows[pos].Cells["colDir"].Value = imageList1.Images[3];
            dataGridViewMessages.Rows[pos].Cells["colStatus"].Value = imageList1.Images[0];
            dataGridViewMessages.Rows[pos].Cells["colID"].Value = value.CommandID.ToString();
            dataGridViewMessages.Rows[pos].Cells["colTextSend"].Value = value.CommandText;

            if (cbScroll.Checked) dataGridViewMessages.FirstDisplayedScrollingRowIndex = dataGridViewMessages.RowCount - 1;
        }

        private void TableSendCommand(DataCommand value)
        {
            // проверим необходимость вывода сообщений
            if (value.CommandSource == eSourceData.refreshInfo && !cbquerty.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.appCommand && !cbGUI.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.userCommand && !cbUserCommand.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.SenderGkode && !cbGcodeSender.Checked) return; //не нужно выводить данное сообщение

            if (InvokeRequired)
            {
                this.Invoke(new Action<DataCommand>(TableSendCommand), new object[] { value });
                return;
            }

            //TODO: Искать лучьше с конца
            int rowIndexFind = -1;
            string sFind = value.CommandID.ToString();

            for (int indxRow = 0; indxRow < dataGridViewMessages.Rows.Count; indxRow++)
            {
                if (dataGridViewMessages.Rows[indxRow].Cells["colID"].Value.ToString() == sFind)
                {
                    rowIndexFind = indxRow;
                    break;
                }
            }

            if (rowIndexFind == -1) return;

            dataGridViewMessages.Rows[rowIndexFind].Cells["colStatus"].Value = imageList1.Images[1];
        }

        private void TableRecivedCommand(DataCommand value)
        {
            if (value ==null) return;

            // проверим необходимость вывода сообщений
            if (value.CommandSource == eSourceData.refreshInfo && !cbquerty.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.appCommand && !cbGUI.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.userCommand && !cbUserCommand.Checked) return; //не нужно выводить данное сообщение
            if (value.CommandSource == eSourceData.SenderGkode && !cbGcodeSender.Checked) return; //не нужно выводить данное сообщение

            if (InvokeRequired)
            {
                this.Invoke(new Action<DataCommand>(TableRecivedCommand), new object[] { value });
                return;
            }

            //TODO: Искать лучьше с конца
            int rowIndexFind = -1;
            string sFind = value.CommandID.ToString();

            for (int indxRow = 0; indxRow < dataGridViewMessages.Rows.Count; indxRow++)
            {
                if (dataGridViewMessages.Rows[indxRow].Cells["colID"].Value.ToString() == sFind)
                {
                    rowIndexFind = indxRow;
                    break;
                }
            }

            if (rowIndexFind == -1)
            {
                //провтыкали.... добавим
                int pos = dataGridViewMessages.Rows.Add();
                dataGridViewMessages.Rows[pos].Cells["colDir"].Value = imageList1.Images[4];
                dataGridViewMessages.Rows[pos].Cells["colStatus"].Value = imageList1.Images[2];
                dataGridViewMessages.Rows[pos].Cells["colID"].Value = value.CommandID.ToString();
                dataGridViewMessages.Rows[pos].Cells["colTextSend"].Value = value.CommandText;
                dataGridViewMessages.Rows[pos].Cells["colTextRecived"].Value = value.ResultCommand;
                if (cbScroll.Checked) dataGridViewMessages.FirstDisplayedScrollingRowIndex = dataGridViewMessages.RowCount - 1;
            }
            else
            {
                dataGridViewMessages.Rows[rowIndexFind].Cells["colDir"].Value = imageList1.Images[4];
                dataGridViewMessages.Rows[rowIndexFind].Cells["colStatus"].Value = imageList1.Images[2];

                string descriptions = "";

                if (value.ResultCommand.StartsWith("ERROR:"))
                {
                    descriptions = @" (" + GRBL.GetErrorDescription((value.ResultCommand.Replace("ERROR:", "").Trim())) + ")";

                }

                dataGridViewMessages.Rows[rowIndexFind].Cells["colTextRecived"].Value = value.ResultCommand + descriptions;
            }


        }

        private void TableAddOther(string value)
        {
            if (!cbOtherMessage.Checked) return; //не нужно выводить данное сообщение

            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(TableAddOther), new object[] { value });
                return;
            }

            int pos = dataGridViewMessages.Rows.Add();
            dataGridViewMessages.Rows[pos].Cells["colDir"].Value = imageList1.Images[5];
            dataGridViewMessages.Rows[pos].Cells["colStatus"].Value = imageList1.Images[2];
            dataGridViewMessages.Rows[pos].Cells["colID"].Value = "";
            dataGridViewMessages.Rows[pos].Cells["colTextSend"].Value = "";
            dataGridViewMessages.Rows[pos].Cells["colTextRecived"].Value = value;
            if (cbScroll.Checked) dataGridViewMessages.FirstDisplayedScrollingRowIndex = dataGridViewMessages.RowCount - 1;
        }

        //---------------------------

        private void GRBL_EvRefreshCurentModeParameters(object sender, EventArgs e)
        {
            this.Invoke(new EventHandler(RefreshCurentModeParameters));
        }

        private void RefreshCurentModeParameters(object sender, EventArgs e)
        {
            refreshMP();
        }

        private void refreshMP()
        {
            lbMotionMode.Text  = @"Motion Mode: " + GRBL.PS.MotionMode.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Coordinate System Select: " + GRBL.PS.CoordinateSystem.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Plane Select: " + GRBL.PS.PlaneSelect.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Units Mode: " + GRBL.PS.UnitMode.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Distance Mode: " + GRBL.PS.DistanceMode.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Feed Rate Mode: " + GRBL.PS.FeedRateMode.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Spindle State: " + GRBL.PS.SpindleState.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Coolant State: " + GRBL.PS.CoolantState.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Tools number: " + GRBL.PS.ToolsNumber.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"S value: " + GRBL.PS.Svalue.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"F value: " + GRBL.PS.Fvalue.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Arc IJK Distance Mode: " + GRBL.PS.ArcIJKDistanceMode.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Tool Length Offset: " + GRBL.PS.ToolLenghtOffset.ToString() + Environment.NewLine;
            lbMotionMode.Text += @"Program Mode: " + GRBL.PS.ProgramMode.ToString() + Environment.NewLine;

        }

        //---------------------------

        private void GRBL_evInfo(object sender, Controller.EventArgsInfo e)
        {
            AppendMessageFromGRBL(e.Message);
        }
        
        public void AppendMessageFromGRBL(string value)
        {
            try
            {
                if (InvokeRequired)
                {
                    this.Invoke(new Action<string>(AppendMessageFromGRBL), new object[] { value });
                    return;
                }

                listGRBLMessages.Items.Add(value);
            }
            catch (Exception e)
            {
   

            }
        }


        private void GRBL_EvTouchPin(object sender, EventArgs e)
        {
            this.Invoke(new EventHandler(RefreshProbePin));
        }


        private void RefreshProbePin(object sender, EventArgs e)
        {
            probePos.Text = GRBL.SETTING.pos_ProbePin.ToString();
            probePosStop.Text = GRBL.SETTING.pos_Works.ToString();
            probePosDiff.Text = (GRBL.SETTING.pos_Works.Z - GRBL.SETTING.pos_ProbePin.Z).ToString();
        }

        #endregion








































        
        #region Процедуры/функции вызываемые из событий

        





        /// <summary>
        /// Индекс для сопоставления принятых сообщений
        /// </summary>
       // private int GUI_indTableMess = 0;

        //private void RefreshTableExchange(object sender, EventArgs e)
        //{
        //    //получим сообщения которые отправили в контроллер
        //    //while (GRBL.DataExchangeManager.CountOutgoing() > 0)
        //    //{
        //    //    int ind = dataGridView1.Rows.Add();
        //    //    dataGridView1.Rows[ind].Cells[0].Value = ind.ToString();
        //    //    dataGridView1.Rows[ind].Cells[1].Value = GRBL.DataExchangeManager.GetOutgoing();
        //    //    dataGridView1.Rows[ind].Cells[2].Value = "";
        //    //}

        //    //получим ответы от контроллера, на отправленные сообщения
        //    //while (GRBL.DataExchangeManager.CountInbox() > 0)
        //    //{
        //    //    string ss = GRBL.DataExchangeManager.GetInbox();

        //    //    if (GUI_indTableMess < dataGridView1.Rows.Count)
        //    //    {
        //    //         dataGridView1.Rows[GUI_indTableMess].Cells[2].Value = ss;
        //    //        GUI_indTableMess++;


        //    //    }
        //    //}


        //    try
        //    {




        //        //if (GRBL.MessageModule.arrayMessages.Count > GRBL.MessageModule.indexReadSendMessage)
        //        //{
        //        //    




        //        //    GRBL.MessageModule.indexReadSendMessage++;
        //        //}

        //        //if (GRBL.MessageModule.indexReadResponceMessage < GRBL.MessageModule.lastResponseIndex)
        //        //{

        //        //    dataGridView1.Rows[(int)GRBL.MessageModule.indexReadResponceMessage].Cells[2].Value = GRBL.MessageModule.arrayMessages[GRBL.MessageModule.indexReadResponceMessage].messageResponse;

        //        //    GRBL.MessageModule.indexReadResponceMessage++;
        //        //}

        //    }
        //    catch (Exception exception)
        //    {
        //        //Console.WriteLine(exception);
        //        //throw;
        //    }


        //}
        


        #endregion

        #region Левая панель

        private void ListAvablePorts_DropDown(object sender, EventArgs e)
        {
            RefreshListAvablePorts();
        }

        private void btReloadListAvablePorts_Click(object sender, EventArgs e)
        {
            RefreshListAvablePorts();
        }

        private void RefreshListAvablePorts()
        {
            if (GRBL == null) return;

            if (GRBL.IsConnected) return; //пока установлена связь обновлять не будем

            ListAvablePorts.Items.Clear();

            List<string> svalues = GRBL.GetListPortName();

            foreach (string svalue in svalues)
            {
                ListAvablePorts.Items.Add(svalue);
            }

            if (ListAvablePorts.Items.Count > 0) ListAvablePorts.SelectedItem = ListAvablePorts.Items[0];

            ListAvableSpeed.SelectedItem = ListAvableSpeed.Items[0];
        }

        private void btConnectDisconnect_Click(object sender, EventArgs e)
        {
            labelLostConnect.Visible = false;
            progressBarBufferSize.Value = 0;
            progressBarBufferSize.Maximum = 0;


            if (GRBL.IsConnected) GRBL.Disconect();
            else
            {
                int speed = 115200;

                int.TryParse(ListAvableSpeed.SelectedItem.ToString(), out speed);

                if (ListAvablePorts.Items.Count == 0)
                {
                    MessageBox.Show("Не выбран порт для подключения!!!");
                    labelErrorConnect.Visible = true;
                    return;
                }

                GRBL.Connect(ListAvablePorts.SelectedItem.ToString(),speed, checkBoxReset.Checked);

                dataGridViewMessages.Rows.Clear();
            }
        }





        private void selectedARDUINO_CheckedChanged(object sender, EventArgs e)
        {
            //tb_pos_w_A.Enabled = false;
            //tb_pos_m_A.Enabled = false;
            p103_StepA.Enabled = false;
            p113_MaxRateA.Enabled = false;
            p123_AccelerationA.Enabled = false;
            p133_MaxTravelA.Enabled = false;
            label6.Enabled = false;
        }

        private void selectedSTM32_CheckedChanged(object sender, EventArgs e)
        {
            //tb_pos_w_A.Enabled = true;
            //tb_pos_m_A.Enabled = true;
            p103_StepA.Enabled = true;
            p113_MaxRateA.Enabled = true;
            p123_AccelerationA.Enabled = true;
            p133_MaxTravelA.Enabled = true;
            label6.Enabled = true;
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (GRBL == null) return;

            GRBL.SETTING.IntervalRefreshPos = (int) numTimeRefresh.Value;
            cbStopTimeRefresh.Checked = false;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (cbStopTimeRefresh.Checked)
            {
                GRBL.SETTING.IntervalRefreshPos = 0;
            }
            else
            {
                GRBL.SETTING.IntervalRefreshPos = (int)numTimeRefresh.Value;
            }
        }


        #endregion

        #region Верхняя область отправки

        private void btSendToController_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand(textBoxSendToControoler.Text, eSourceData.userCommand);
            textBoxSendToControoler.Text = @"";
        }

        private void textBoxSendToControoler_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                GRBL.SendCommand(textBoxSendToControoler.Text, eSourceData.userCommand);
                textBoxSendToControoler.Text = @"";
            }
        }        

        #endregion

        #region Страница настроек контроллера

        private void btReadSetting_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$$", eSourceData.appCommand);
            // Это для того, что-бы пользователь случайно не нажал записать, до того как текущие настройки не заполнят поля на форме.
            // иначе в контроллер попадут значения из незаполненных элементов настроек!!!!!
            btWriteSetting.Enabled = true;
        }

        private void btWriteSetting_Click(object sender, EventArgs e)
        {

            string _decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            GRBL.SendCommand("$0=" + p00_StepPulse.Value.ToString(), eSourceData.appCommand);
            GRBL.SendCommand("$1=" + p01_StepIdleDelay.Value.ToString(), eSourceData.appCommand);
            GRBL.SendCommand("$2=" + p02_StepPortInvertMask.GetValue().ToString(), eSourceData.appCommand);
            GRBL.SendCommand("$3=" + p03_DirectionPortInvertMask.GetValue().ToString(), eSourceData.appCommand);
            GRBL.SendCommand("$4="  + (p04_StepEnableInvert.Checked ? "1" : "0"), eSourceData.appCommand);
            GRBL.SendCommand("$5="  + (p05_LimitPinsInvert.Checked  ? "1" : "0"), eSourceData.appCommand);
            GRBL.SendCommand("$6="  + (p06_ProbePinInvert.Checked   ? "1" : "0"), eSourceData.appCommand);
            //10 параметр НЕТРОГАЕМ!!!!
            GRBL.SendCommand("$11=" + p11_JunctionDeviation.Value.ToString().Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$12=" + p12_ArcTolerance.Value.ToString().Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$13=" + (p13_ReportInches.Checked ? "1" : "0"), eSourceData.appCommand);
            GRBL.SendCommand("$20=" + (p20_SoftLimit.Checked ? "1" : "0"), eSourceData.appCommand);
            GRBL.SendCommand("$21=" + (p21_HardLimit.Checked ? "1" : "0"), eSourceData.appCommand);
            //22
            //23
            //24
            //25
            //26
            //27
            //30
            //31
            GRBL.SendCommand("$32=" + (p32_LaserMode.Checked        ? "1" : "0"), eSourceData.appCommand);


            GRBL.SendCommand("$100=" + p100_StepX.Value.ToString(        "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$110=" + p110_MaxRateX.Value.ToString(     "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$120=" + p120_AccelerationX.Value.ToString("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$130=" + p130_MaxTravelX.Value.ToString(   "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);

            GRBL.SendCommand("$101=" + p101_StepY.Value.ToString(        "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$111=" + p111_MaxRateY.Value.ToString(     "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$121=" + p121_AccelerationY.Value.ToString("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$131=" + p131_MaxTravelY.Value.ToString(   "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);

            GRBL.SendCommand("$102=" + p102_StepZ.Value.ToString(        "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$112=" + p112_MaxRateZ.Value.ToString(     "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$122=" + p122_AccelerationZ.Value.ToString("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            GRBL.SendCommand("$132=" + p132_MaxTravelZ.Value.ToString(   "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);

            if (GRBL.SETTING.CountAxes == AxisVariant.XYZA || GRBL.SETTING.CountAxes == AxisVariant.XYZAB)
            {
                GRBL.SendCommand("$103=" + p103_StepA.Value.ToString        ("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
                GRBL.SendCommand("$113=" + p113_MaxRateA.Value.ToString     ("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
                GRBL.SendCommand("$123=" + p123_AccelerationA.Value.ToString("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
                GRBL.SendCommand("$133=" + p133_MaxTravelA.Value.ToString   ("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            }

            if (GRBL.SETTING.CountAxes == AxisVariant.XYZAB)
            {
                GRBL.SendCommand("$104=" + p104_StepB.Value.ToString(        "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
                GRBL.SendCommand("$114=" + p114_MaxRateB.Value.ToString(     "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
                GRBL.SendCommand("$124=" + p124_AccelerationB.Value.ToString("#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
                GRBL.SendCommand("$134=" + p134_MaxTravelB.Value.ToString(   "#0.###").Replace(_decimalSeparator, "."), eSourceData.appCommand);
            }



        }

        #endregion

        #region Ручное управление JOG

        private void button19_Click(object sender, EventArgs e)
        {
            //$J=X10.0 Y-1.5 F100

            GRBL.SendCommand("$J=G91 X-" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button17_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 X" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button21_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 Y-" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" +numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 Y" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" +numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button18_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 Z-" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" +numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button20_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 Z" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button22_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 A-" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button23_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 A" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button24_Click(object sender, EventArgs e)
        {
            // отмена движения запущенного вручную
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.JogCancel);
        }

        #endregion

        #region Ручное управление Touchpin

            private void btStartProbeTouch_Click(object sender, EventArgs e)
            {
                probePos.Text = @"";
                string _decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

                string sSpeed   = numTouchSpeed.Value.ToString("#0.###").Replace(_decimalSeparator, ".");
                string sDistance = numTouchmaxZ.Value.ToString("#0.###").Replace(_decimalSeparator, ".");

                GRBL.SendCommand("G38.2 F" + sSpeed + " Z-" + sDistance, eSourceData.appCommand);
            }

        #endregion
        
        #region разное


        //очистка списка на первой странице
        private void btClearMessage_Click(object sender, EventArgs e)
        {
            listGRBLMessages.Items.Clear();
        }

        // и на второй странице
        private void button12_Click(object sender, EventArgs e)
        {
            dataGridViewMessages.Rows.Clear();
        }

        // установка альтернативных координат
        private void button13_Click(object sender, EventArgs e)
        {
            string _decimalSeparator = System.Threading.Thread.CurrentThread.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            string sx = newX.Value.ToString().Replace(_decimalSeparator, ".");
            string sy = newY.Value.ToString().Replace(_decimalSeparator, ".");
            string sz = newZ.Value.ToString().Replace(_decimalSeparator, ".");
            string sa = newA.Value.ToString().Replace(_decimalSeparator, ".");

            if (selectedSTM32.Checked)
            {
                GRBL.SendCommand("G90 G10 L20 P0 X" + sx + " Y" + sy + " Z" + sz + " A" + sa, eSourceData.appCommand);
            }
            else //if arduino
            {
                GRBL.SendCommand("G90 G10 L20 P0 X" + sx + " Y" + sy + " Z" + sz, eSourceData.appCommand);
            }
        }



        #endregion

        #region Различные управляюще команды

        private void KillAlarmLock_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.KillAlarmLock); // сброс статуса авария
        }

        private void SoftReset_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.SoftReset);
        }

        private void SLEEP_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.SLEEP);
        }

        private void query_POS_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand(ControllerCommand.query_POS);
        }

        private void ResetSetting_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.Reset_ControllerSetting); // сброс пользовательских настроек
        }

        private void ResetPOS_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.Reset_Coordinates); //Сброс координат G54,G55,G56,G57,G58,G59
        }

        private void FullReset_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.Reset_All); // Полный сброс всех настроек координат
        }

        private void TestMode_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.TestMode); // вкл/вывл режима тестирования
        }

        private void AboutFirmware_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand(ControllerCommand.AboutFirmware); // запрос информации о контроллере
        }

        private void ToHome_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand(ControllerCommand.ToHome); // поиск дома
        }

        private void Hold_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.Hold); // остановка выполнения
        }

        private void StartResume_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.StartResume); // запуск продолжения
        }

        private void GetPOS_Click(object sender, EventArgs e)
        {
            //запрос доп параметров
            GRBL.SendRealTimeCommand(ControllerCommand.query_POS);
        }

        private void GetParsing_Click(object sender, EventArgs e)
        {
            //запрос режимов/параметров контроллера
            GRBL.SendRealTimeCommand(ControllerCommand.query_Parsing);
        }
        // идеинтичная кнопка
        private void button4_Click_1(object sender, EventArgs e)
        {
            GRBL.SendCommand(ControllerCommand.query_Parsing);
        }

        private void buttonOverSpeed1_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.FeedOverridesSet100);
        }

        private void buttonOverSpeed2_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.FeedOverridesDecrease1);
        }

        private void buttonOverSpeed3_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.FeedOverridesIncrease1);
        }

        private void buttonOverSpeed4_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.FeedOverridesDecrease10);
        }

        private void buttonOverSpeed5_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.FeedOverridesIncrease10);
        }

        private void btOverPower1_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.SpindleSpeedOverridesSet100);
        }

        private void btOverPower2_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.SpindleSpeedOverridesDecrease1);
        }

        private void btOverPower3_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.SpindleSpeedOverridesIncrease1);
        }

        private void btOverPower4_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.SpindleSpeedOverridesDecrease10);
        }

        private void btOverPower5_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerRealTimeCommand.SpindleSpeedOverridesIncrease10);
        }





        #endregion

        #region Блок отправки G-кода в контроллер

        private void btLoadFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Title = @"Выбор файла с G-кодом",
                Filter = @"Файл - nc (*.nc)|*.nc|Все файлы (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            dataGridListGcodes.Rows.Clear();

            var fileStream = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    int indx = dataGridListGcodes.Rows.Add();

                    dataGridListGcodes.Rows[indx].Cells["IndRow"].Value      = indx+1;
                    dataGridListGcodes.Rows[indx].Cells["CommandLine"].Value = line;
                    dataGridListGcodes.Rows[indx].Cells["ResultSend"].Value  = "";
                }
            }
        }

        private void button30_Click(object sender, EventArgs e)
        {
            dataGridListGcodes.Rows.Clear();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            if (backgroundSender.IsBusy)
            {
                MessageBox.Show("Поток отправки, ещё не завершился!!!");
                return;
            }

            dataGridListGcodes.ReadOnly = true;


            progressBar1.Value = 0;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = dataGridListGcodes.RowCount;


            for (int i = 0; i < dataGridListGcodes.RowCount-1; i++)
            {
                dataGridListGcodes.Rows[i].Cells["ResultSend"].Value = "";
            }

            backgroundSender.RunWorkerAsync();
        }


        private void button28_Click(object sender, EventArgs e)
        {
            thExit = true;
            dataGridListGcodes.ReadOnly = false;
        }

        private volatile bool thExit;
        private volatile int indxRow;
       

        private void backgroundSender_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            indxRow = 0;
            thExit = false;

            while (indxRow < dataGridListGcodes.RowCount-1 && !thExit)
            {
                Thread.Sleep(100);

                //TODO: в будущем учеть возможность доработки данного условия
                if (GRBL.DM.CountCommand_Send > 10) continue; //не будем в буфер слать команды пока стек немного не уменьшится

                string strSend = dataGridListGcodes.Rows[indxRow++].Cells["CommandLine"].Value.ToString();

                GRBL.SendCommand(strSend,eSourceData.SenderGkode);

                backgroundSender.ReportProgress(indxRow);

            }
        }

        private void backgroundSender_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progressGUI(e.ProgressPercentage);
        }

        private void progressGUI(int value)
        {
           

            if (InvokeRequired)
            {
                this.Invoke(new Action<int>(progressGUI), new object[] { value });
                return;
            }


            progressBar1.Value = value;
            label76.Text = "Отправлено: " + indxRow.ToString() + " из: " + dataGridListGcodes.RowCount;
            dataGridListGcodes.Rows[indxRow-1].Cells["ResultSend"].Value = "отправлено";

            if (checkBox1.Checked) dataGridListGcodes.FirstDisplayedScrollingRowIndex = indxRow - 1;

        }





        private void backgroundSender_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {

        }





        #endregion

        private void timer1_Tick(object sender, EventArgs e)
        {
            label77.Text = @"Для отправки: " + GRBL.DM.CountCommand_Send.ToString() + Environment.NewLine
                         + @"Отправлено: " + GRBL.DM.CountCommand_Sending.ToString() + Environment.NewLine
                         + @"Получено: " + GRBL.DM.CountCommand_Reception.ToString() + Environment.NewLine;

        }

        private void label77_Click(object sender, EventArgs e)
        {

        }

        private void numTimeRefreshStatus_ValueChanged(object sender, EventArgs e)
        {
            if (GRBL == null) return;

            GRBL.SETTING.IntervalRefreshStatus = (int)numTimeRefreshStatus.Value;
            checkBox2.Checked = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                GRBL.SETTING.IntervalRefreshStatus = 0;
            }
            else
            {
                GRBL.SETTING.IntervalRefreshStatus = (int)numTimeRefreshStatus.Value;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
           GRBL.SendRealTimeCommand(ControllerRealTimeCommand.StatusReportQuery);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand(ControllerCommand.query_setting); // запуск продолжения
        }

        private void button3_Click(object sender, EventArgs e)
        {
            GRBL.SendRealTimeCommand("$10=2"); // запуск продолжения
        }

        private void button5_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 B" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            GRBL.SendCommand("$J=G91 B-" + numDistanceJog.Value.ToString("#0.###").Replace(',', '.') + " F" + numSpeedJog.Value.ToString().Replace(',', '.'), eSourceData.appCommand);
            GRBL.SendCommand("G90", eSourceData.appCommand);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            dataGridScan.Rows.Clear();
            dataGridScan.Columns.Clear();

            if (numScanX.Value < 1 || numScanY.Value < 1 || numStepX.Value < 1 || numStepY.Value < 1) return;

            dataGridScan.Columns.Add("NamePoint", "NamePoint");

            for (int x = 0; x < numScanX.Value+1; x+=(int)numStepX.Value)
            {
                dataGridScan.Columns.Add("posX_" +x, "X: " + x);
            }



            for (int y = 0; y < numScanY.Value+1; y+=(int)numStepY.Value)
            {
                   int indx = dataGridScan.Rows.Add();

                    dataGridScan.Rows[indx].Cells[0].Value = "Y: " + y;

                    for (int x = 0; x < numScanX.Value+1; x += (int)numStepX.Value)
                    {
                        dataGridScan.Rows[indx].Cells[x+1].Value = 0;
                    }
            }








        }
    }
}
