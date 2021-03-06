﻿Imports System.IO.Ports

Public Class Form


    Delegate Sub ConsolleWriteDelegate(ByVal [text] As String)
    Delegate Sub RXDataTBWriteDelegate(ByVal [val] As Integer)
    Delegate Sub TXDataTBWriteDelegate(ByVal [val] As Integer)

    Private WithEvents sockServer As AsynchronousSocketListener
    Private WithEvents sockClient As AsynchronousClient
    Private WithEvents serialPort As New SerialPort

    Private lastConsoleMsg As String = ""
    Private lastConsoleMsgTime As Date
    Private m_AppConfig As AppSettings
    Private m_fConsole As Boolean = False

    Const WINDOW_HEIGHT_BIG = 750
    Const WINDOW_HEIGHT_SMALL = 330

    Private Shared fServer As Boolean
    Private m_TCP_SendTimeout As Integer = 3000
    Private m_TCP_fNoDelay As Boolean = True

    Private Sub Form_Load(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MyBase.Load
        Me.Client.Visible = False
        Me.BtnClose.Enabled = False
        Me.GetCOMPB.Visible = False

        m_AppConfig = New AppSettings(AppSettings.Config.SharedFile)

        loadSerialPortInfo()
        LoadConfiguration()

        Dim portName, baudRate, cbxState As String
        portName = m_AppConfig.GetSetting("PortName")
        baudRate = m_AppConfig.GetSetting("BaudRate")
        cbxState = m_AppConfig.GetSetting("AutoStart")



        Me.BaudRateTB.Text = baudRate
        Try
            Me.PortNameCB.SelectedIndex = Me.PortNameCB.FindStringExact(portName)
            If cbxState.Equals("Y") Then
                Me.cbx1.Checked = True
                startServer()
            Else
                Me.cbx1.Checked = False
            End If
        Catch ex As Exception
            'MessageBox.Show(ex.Message)
            MessageBox.Show("Port not found!")
        End Try

        Dim strText As String = "Port not found!"
        Dim time As String
        time = TimeOfDay.TimeOfDay.ToString()

        strText = time + ":   " + strText
        consoleWrite(strText)

        Me.Height = WINDOW_HEIGHT_SMALL
        Me.Width = 500

    End Sub


    Private Sub loadSerialPortInfo()
        ' Allow the user to set the appropriate properties.
        setPortNameCB()
        setBaudRateCB()
        setParityCB()
        setDataBitsCB()
        setStopBitsCB()
        setHandshakeCB()
    End Sub
    Private Sub setHandshakeCB()
        Dim s As String
        For Each s In [Enum].GetNames(GetType(Handshake))
            Me.HandshakeCB.Items.Add(s)
        Next s
        Me.HandshakeCB.SelectedText = [Enum].GetName(GetType(Handshake), Me.serialPort.Handshake)
    End Sub
    Private Sub setStopBitsCB()

        Dim s As String
        For Each s In [Enum].GetNames(GetType(StopBits))
            Me.StopBitsCB.Items.Add(s)
        Next s

        Me.StopBitsCB.SelectedText = [Enum].GetName(GetType(StopBits), Me.serialPort.StopBits)
    End Sub
    Private Sub setDataBitsCB()
        Me.DataBitsTB.Text = serialPort.DataBits.ToString
    End Sub
    Private Sub setParityCB()
        Dim s As String
        For Each s In [Enum].GetNames(GetType(Parity))
            Me.ParityCB.Items.Add(s)
        Next s
        Me.ParityCB.SelectedText = [Enum].GetName(GetType(Parity), Me.serialPort.Parity)
    End Sub
    Private Sub setBaudRateCB()
        Me.BaudRateTB.Text = serialPort.BaudRate.ToString
    End Sub
    Private Sub setPortNameCB()
        Dim s As String
        Try
            For Each s In serialPort.GetPortNames()
                Me.PortNameCB.Items.Add(s)
            Next s
            Me.PortNameCB.SelectedIndex = 0
        Catch ex As Exception
            'MessageBox.Show(ex.Message)
            MessageBox.Show("Port not found!")
        End Try

    End Sub

    Private Sub Client_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Client.Click
        SaveCurrentConfiguration()
        disableForm()
        fServer = False
        sockClient = New AsynchronousClient(Me.ServerIPAddr.Text, CInt(Me.ServerPort.Text))
        AsynchronousClient.m_fNoDelay = m_TCP_fNoDelay
        AsynchronousClient.m_SendTimeout = m_TCP_SendTimeout
        sockClient.Start()

    End Sub

    Private Sub startServer()
        SaveCurrentConfiguration()
        disableForm()
        startCom()
        fServer = True
        sockServer = New AsynchronousSocketListener(Me.ServerIPAddr.Text, CInt(Me.ServerPort.Text))
        AsynchronousSocketListener.m_fNoDelay = m_TCP_fNoDelay
        AsynchronousSocketListener.m_SendTimeout = m_TCP_SendTimeout
        sockServer.Start()
        Me.BtnClose.Enabled = True
    End Sub
    Private Sub Server_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Server.Click
        startServer()
    End Sub

    Private Sub SaveCurrentConfiguration()
        m_AppConfig.SaveSetting("IPAddress", Me.ServerIPAddr.Text)
        m_AppConfig.SaveSetting("Port", Me.ServerPort.Text)
        m_AppConfig.SaveSetting("PortName", Me.PortNameCB.Text)
        m_AppConfig.SaveSetting("BaudRate", Me.BaudRateTB.Text)
        Dim scbx As String
        If cbx1.Checked = True Then
            scbx = "Y"
        Else
            scbx = "N"
        End If
        m_AppConfig.SaveSetting("AutoStart", scbx)
    End Sub

    Private Sub LoadConfiguration()
        Dim strTmp As String

        strTmp = m_AppConfig.GetSetting("IPAddress")

        If strTmp Is Nothing Then
            strTmp = "127.0.0.1"
        End If
        Me.ServerIPAddr.Text = strTmp

        strTmp = m_AppConfig.GetSetting("Port")
        If strTmp Is Nothing Then
            strTmp = "8000"
        End If
        Me.ServerPort.Text = strTmp

    End Sub

    Private Sub addRXDataTBValue(ByVal dataRecived As Integer)
        If Me.InvokeRequired Then
            Dim d As New RXDataTBWriteDelegate(AddressOf addRXDataTBValue)
            Me.Invoke(d, New Object() {dataRecived})
        Else
            Me.RXDataTB.Text = CStr(CInt(Me.RXDataTB.Text) + dataRecived)
        End If
    End Sub
    Private Sub addTXDataTBValue(ByVal dataTransmitted As Integer)
        If Me.InvokeRequired Then
            Dim d As New TXDataTBWriteDelegate(AddressOf addTXDataTBValue)
            Me.Invoke(d, New Object() {dataTransmitted})
        Else
            Me.TXDataTB.Text = CStr(CInt(Me.TXDataTB.Text) + dataTransmitted)
        End If
    End Sub
    Private Sub consolleWriteline(ByVal strText As String)
        If strText <> lastConsoleMsg Then
            lastConsoleMsg = strText
            lastConsoleMsgTime = Now()
            consoleWriteEntry(strText)
        Else
            If (DateDiff(DateInterval.Second, lastConsoleMsgTime, Now) > 4) Then
                'la stessa cosa si può scrivere al massimo ogni 4 secondi
                consoleWriteEntry(strText)
                lastConsoleMsg = strText
                lastConsoleMsgTime = Now()
            Else
                consoleWrite(".")
            End If
        End If
    End Sub
    Private Sub consoleWriteEntry(ByVal strText As String)
        Dim time As String = TimeOfDay.TimeOfDay.ToString()
        strText = vbCrLf + time + ":   " + strText
        consoleWrite(strText)
    End Sub

    Private Sub consoleWrite(ByVal strText As String)
        If Me.InvokeRequired Then
            Dim d As New ConsolleWriteDelegate(AddressOf consoleWrite)
            Me.Invoke(d, New Object() {strText})
        Else
            Me.Consolle.AppendText(strText)
        End If
    End Sub
    Private Sub disableForm()
        Me.Server.Enabled = False
        Me.Client.Enabled = False
        Me.ServerIPAddr.Enabled = False
        Me.ServerPort.Enabled = False
    End Sub

    Private Sub enableForm()
        Me.Server.Enabled = True
        Me.Client.Enabled = True
        Me.ServerIPAddr.Enabled = True
        Me.ServerPort.Enabled = True
    End Sub

    Private Sub Form_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        If (Not IsNothing(sockServer)) Then
            sockServer.StopListening()
            sockServer = Nothing
        End If
    End Sub
    Private Sub Form_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Leave

    End Sub

    Private Sub startCom()
        Try
            Dim strTmp As String
            
            Me.GroupBox2.Enabled = False
            serialPort.PortName = Me.PortNameCB.Text
            serialPort.BaudRate = CInt(Me.BaudRateTB.Text)
            serialPort.Parity = CType([Enum].Parse(GetType(Parity), Me.ParityCB.Text), Parity)
            serialPort.DataBits = CInt(Me.DataBitsTB.Text)
            serialPort.StopBits = CType([Enum].Parse(GetType(StopBits), Me.StopBitsCB.Text), StopBits)
            serialPort.Handshake = CType([Enum].Parse(GetType(Handshake), Me.HandshakeCB.Text), Handshake)
            ' Set the read/write timeouts
            serialPort.ReadTimeout = 500
            serialPort.WriteTimeout = 500

            serialPort.Open()

            consolleWriteline(serialPort.PortName + " correctly opened!")
        Catch ex As Exception
            consolleWriteline("Unable to open " + Me.PortNameCB.Text + ".")
            Me.GroupBox2.Enabled = True
            MessageBox.Show(ex.Message)

        Finally

        End Try
    End Sub

    Private Sub GetCOMPB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles GetCOMPB.Click
        startCom()
    End Sub


    Private Sub serialPort_DataReceived(ByVal sender As Object, ByVal e As System.IO.Ports.SerialDataReceivedEventArgs) Handles serialPort.DataReceived
        Dim buffSize As Integer = 5000
        Dim buffer(buffSize) As Byte
        Dim index As Integer = 0

        While serialPort.BytesToRead > 0
            buffer(index) = serialPort.ReadByte()
            index = index + 1
        End While

        If fServer Then
            If Not IsNothing(sockServer) Then
                sockServer.SendData(buffer, index)
            Else
                consolleWriteline("COM Listener: Unable to forward data - socket not started!")
            End If
        Else
            If Not IsNothing(sockClient) Then
                sockClient.SendData(buffer, index)
            Else
                consolleWriteline("COM Listener: Unable to forward data - socket not started!")
            End If

        End If
    End Sub

    Private Sub sockServer_dataRecived(ByVal buffer() As Byte, ByVal bytesRecived As Integer) Handles sockServer.dataRecived
        'consolleWriteline("Recived: " + strData)
        addRXDataTBValue(bytesRecived)

        Try
            serialPort.Write(buffer, 0, bytesRecived)
        Catch ex As Exception
            If serialPort.IsOpen Then
                consolleWriteline(ex.ToString)
            Else
                consolleWriteline("SOCK: Unable to write on COM. The port is CLOSED.")
            End If
        End Try
    End Sub
    Private Sub sockServer_dataSent(ByVal buffer() As Byte, ByVal bytesSent As Integer) Handles sockServer.dataSent
        addTXDataTBValue(bytesSent)
    End Sub
    Private Sub sockServer_logEntry(ByVal strData As String) Handles sockServer.logEntry
        TimeOfDay.ToString()
        consolleWriteline(strData)
    End Sub

    Private Sub sockClient_dataRecived(ByVal buffer() As Byte, ByVal bytesRecived As Integer) Handles sockClient.dataRecived
        'consolleWriteline("Recived: " + strData)
        addRXDataTBValue(bytesRecived)
        Try
            serialPort.Write(buffer, 0, bytesRecived)
        Catch ex As Exception
            If serialPort.IsOpen Then
                consolleWriteline(ex.ToString)
            Else
                consolleWriteline("SOCK: Unable to write on COM. The port is CLOSED.")
            End If
        End Try
    End Sub
    Private Sub sockClient_dataSent(ByVal buffer() As Byte, ByVal bytesSent As Integer) Handles sockClient.dataSent
        addTXDataTBValue(bytesSent)
    End Sub
    Private Sub sockclient_logEntry(ByVal strData As String) Handles sockClient.logEntry
        TimeOfDay.ToString()
        consolleWriteline(strData)
    End Sub

    Private Sub sockClient_serverNotReachable(ByVal sSocket As System.Net.Sockets.Socket) Handles sockClient.serverNotReachable
        consolleWriteline("Server NOT Reachable")
        sockClient.RetryConnection()
    End Sub

    Private Sub InfoToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MainMenu_Info.Click
        Dim ab As New AboutBox
        ' ab.Show()
        MessageBox.Show("This is software for multi purpose.")

    End Sub
    Private Sub ConsolePB_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ConsolePB.Click
        If m_fConsole Then
            Me.Height = WINDOW_HEIGHT_SMALL
            Me.Width = 500
            Me.ConsolePB.Text = "Console >>"
        Else
            Me.Height = WINDOW_HEIGHT_BIG
            Me.Width = 500
            Me.ConsolePB.Text = "Console <<"
        End If
        m_fConsole = Not m_fConsole
    End Sub
    Private Sub ExitToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ExitToolStripMenuItem.Click
        Dim settings As New TCPSettings
        settings.SendTimeoutTB.Text = m_TCP_SendTimeout
        settings.fNoDelay.Checked = m_TCP_fNoDelay

        settings.ShowDialog()
        If settings.DialogResult = Windows.Forms.DialogResult.OK Then

            m_TCP_SendTimeout = settings.SendTimeoutTB.Text
            m_TCP_fNoDelay = settings.fNoDelay.Checked

        End If
    End Sub

    Private Sub ServerIPAddr_TextChanged(sender As Object, e As EventArgs) Handles ServerIPAddr.TextChanged

    End Sub

    Private Sub BtnClose_Click(sender As Object, e As EventArgs) Handles BtnClose.Click
        Dim result As Integer = MessageBox.Show("Are you sure you want to stop server & exit ?", "Information Exit", MessageBoxButtons.YesNoCancel)
        If result = DialogResult.Yes Then
            Application.Exit()
        End If

    End Sub

    Private Sub ExitToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem1.Click

    End Sub

    Private Sub cbx1_CheckedChanged(sender As Object, e As EventArgs) Handles cbx1.CheckedChanged

    End Sub
End Class
