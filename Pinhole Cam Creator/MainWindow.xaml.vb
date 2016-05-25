Imports LightTools
Imports LTCOM64
Imports LTJumpStartNET
Imports LTLOCATORLib
Imports System.ComponentModel
Imports System.Math



Class MainWindow
    Dim ltLoc As New Locator
    Dim lt As LTAPI
    Dim js As New LTCOM64.JSNET
    Dim pID As Integer
    Dim stat As LTReturnCodeEnum
    'variables for pinhole cam creation
    Dim newDummyPlaneKey As String
    Dim newDummyPlaneName As String
    Dim newCamKey As String
    Dim newCamName As String
    Dim newGroupKey, newGroupName As String
    Dim debugMode As Boolean

    Public Sub New()
        ' 設計工具需要此呼叫。
        InitializeComponent()
        ' 在 InitializeComponent() 呼叫之後加入所有初始設定。
        debugMode = False
        'debugMode = True
        lt = LTGetter.getLTAPIServer
        If debugMode Then
            If IsNothing(lt) Then
                MsgBox("LightTools session not found!")
            Else
                Me.Title = "Pinhole Camera Creator (PID = " + Str(lt.GetServerID()) + ")"
                Call initSettings()
            End If
        Else
            pID = 7540
            lt = ltLoc.GetLTAPI(pID)
        End If
    End Sub


    Private Sub calcDimension()
        'calculate dummy plane dimensions according to PRR/Lum. mesh settings
        Dim fov, arW, arH As Double
        Dim imgWidth, imgHeight, workDist As Double
        workDist = CDbl(Me.workDist.Text)
        arW = CDbl(Me.arW.Text)
        arH = CDbl(Me.arH.Text)
        fov = CDbl(Me.fov.Text)
        imgHeight = 2 * workDist * Math.Tan((fov * Math.PI / 180) / 2)
        imgWidth = imgHeight * arW / arH
        Me.imgHeight.Text = imgHeight
        Me.imgWidth.Text = imgWidth
    End Sub
    Private Function calcMinMeshStr(meshWidth As Double, meshHeight As Double, xNum As Integer, yNum As Integer, workDist As Double)
        'calculate minimal solid angle on the mesh (the one on upper left corner)
        Dim gridWidth, gridHeight As Double
        Dim r, d, s As Double
        Dim minMeshStr As Double
        d = workDist
        gridWidth = meshWidth / xNum
        gridHeight = meshHeight / yNum
        r = Sqrt((meshWidth * 0.5 - gridWidth * 0.5) ^ 2 + (meshHeight * 0.5 - gridHeight * 0.5) ^ 2)
        s = Sqrt(r ^ 2 + d ^ 2)
        minMeshStr = (gridHeight * gridWidth) * d / s ^ 3
        Return minMeshStr
    End Function
    Private Sub updateMinMeshStr()
        Dim minMeshStr As Double
        minMeshStr = calcMinMeshStr(imgWidth.Text, imgHeight.Text, xNum.Text, yNum.Text, workDist.Text)
        minStrOut.Text = String.Format("{0:0.000e00}", minMeshStr)
    End Sub
    Private Sub chkIntInput(sender As Object)
        Dim input As Integer
        If Not Integer.TryParse(DirectCast(sender, TextBox).Text, input) Then
            MsgBox("Integer value is required in this field!")
            DirectCast(sender, TextBox).Undo()
        End If
    End Sub
    Private Sub chkNumInput(sender As Object)
        Dim input As Double
        If Not Double.TryParse(DirectCast(sender, TextBox).Text, input) Then
            MsgBox("Numerical value is required in this field!")
            DirectCast(sender, TextBox).Undo()
        End If
    End Sub
    Private Sub workDist_KeyUp(sender As Object, e As KeyEventArgs) Handles workDist.KeyUp
        Call chkNumInput(sender)
        Call calcDimension()
        Call updateMinMeshStr()
    End Sub

    Private Sub fov_KeyUp(sender As Object, e As KeyEventArgs) Handles fov.KeyUp
        Call chkNumInput(sender)
        Call calcDimension()
        Call updateMinMeshStr()
    End Sub

    Private Sub apSize_KeyUp(sender As Object, e As KeyEventArgs) Handles apSize.KeyUp
        Call chkNumInput(sender)
    End Sub

    Private Sub lookD_KeyUp(sender As Object, e As KeyEventArgs) Handles lookD.KeyUp
        Call chkNumInput(sender)
    End Sub

    Private Sub xNum_KeyUp(sender As Object, e As KeyEventArgs) Handles xNum.KeyUp
        Call chkIntInput(sender)
        Call updateMinMeshStr()
    End Sub

    Private Sub yNum_KeyUp(sender As Object, e As KeyEventArgs) Handles yNum.KeyUp
        Call chkIntInput(sender)
        Call updateMinMeshStr()
    End Sub
    Private Sub arH_KeyUp(sender As Object, e As KeyEventArgs) Handles arH.KeyUp
        Call chkNumInput(sender)
        Call calcDimension()
        Call updateMinMeshStr()
    End Sub

    Private Sub arW_KeyUp(sender As Object, e As KeyEventArgs) Handles arW.KeyUp
        Call chkNumInput(sender)
        Call calcDimension()
        Call updateMinMeshStr()
    End Sub
    Private Sub createCamera(fov As Double, lookDist As Double, arW As Double, arH As Double)
        'create PRR camera
        lt.SetOption("View Update", 0)
        lt.Cmd("\V3D")
        lt.Cmd("PlaceCamera " + js.LTCoord3(0, 0, 0) + " " + js.LTCoord3(0, 0, 1)) 'create a camera at origin
        newCamKey = "LENS_MANAGER[1].STUDIO_MANAGER[Studio_Manager].DATABASE[Camera_List].ENTITY[@Last]"
        newCamName = lt.DbGet(newCamKey, "Name")
        lt.DbSet(newCamKey, "Field_Of_View", fov)
        lt.DbSet(newCamKey, "Look_Distance", lookDist)
        lt.DbSet(newCamKey, "Aspect_Width", arW)
        lt.DbSet(newCamKey, "Aspect_Height", arH)
        lt.DbSet(newCamKey, "Gamma", 180)
    End Sub
    Private Sub createImgPlane(width As Double, height As Double, xNum As Integer, yNum As Integer, workDist As Double, apSize As Double)
        'create image plane for pin hole imaging
        Dim newReceiverKey As String
        Dim newLumMeshKey As String
        lt.SetOption("View Update", 0)
        stat = js.MakeDummyPlane(lt, 0, 0, -workDist, 0, 0, 1, W:=width, H:=height, ApertureType:="Rectangular") 'create dummy plane
        newDummyPlaneKey = "LENS_MANAGER[1].COMPONENTS[Components].PLANE_DUMMY_SURFACE[@Last]"
        newDummyPlaneName = lt.DbGet(newDummyPlaneKey, "Name")
        js.MakeReceiver(lt, EntityName:=newDummyPlaneName, ReceiverName:="Pinhole Image") 'create receiver
        'receiver settings
        newReceiverKey = "LENS_MANAGER[1].ILLUM_MANAGER[Illumination_Manager].RECEIVERS[Receiver_List].SURFACE_RECEIVER[@Last]"
        lt.DbSet(newReceiverKey + ".BACKWARD[Backward_Simulation]", "Has_Spatial_Luminance", "Yes")
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "Meter_Collection_Mode", "Fixed Aperture")
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "VirtualAperture", "Yes")
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "Distance", workDist)
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "Disk_Radius", apSize)
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "Receiver_Units", "Photometric")
        'luminance mesh settings
        newLumMeshKey = newReceiverKey + ".BACKWARD_SPATIAL_LUMINANCE[Spatial_Luminance].SPATIAL_LUMINANCE_MESH[Spatial_Luminance_Mesh]"
        lt.DbSet(newLumMeshKey, "X_Dimension", xNum)
        lt.DbSet(newLumMeshKey, "Y_Dimension", yNum)
        lt.DbSet(newLumMeshKey, "Name", "PinHoleLumMap")
    End Sub
    Private Sub initSettings()
        If Me.IsInitialized Then
            'Luminance mesh settings
            Me.xNum.Text = 200 'number of bins in x-direction
            Me.yNum.Text = 200 'number of bins in y-direction
            Me.workDist.Text = 20 'working distance
            Me.apSize.Text = 0.02 'aperture size
            Me.imgHeight.Text = 40
            Me.imgWidth.Text = 40
            'PRR camera settings
            Me.fov.Text = 90 'FoV
            Me.lookD.Text = 10 'looking distance
            Me.arW.Text = 1 'aspect ratio (width)
            Me.arH.Text = 1 'aspect raito (height)
            Call updateMinMeshStr()
        End If
    End Sub
    Private Sub reset_Click(sender As Object, e As RoutedEventArgs) Handles reset.Click
        Call initSettings()
    End Sub
    Private Sub create_Click(sender As Object, e As RoutedEventArgs) Handles create.Click
        Dim fov, lookD, arW, arH As Double
        Dim imgWidth, imgHeight, workDist, apSize As Double
        Dim xNum, yNum As Integer
        xNum = CInt(Me.xNum.Text)
        yNum = CInt(Me.yNum.Text)
        workDist = CDbl(Me.workDist.Text)
        apSize = CDbl(Me.apSize.Text)
        arW = CDbl(Me.arW.Text)
        arH = CDbl(Me.arH.Text)
        fov = CDbl(Me.fov.Text)
        lookD = CDbl(Me.lookD.Text)
        imgWidth = CDbl(Me.imgWidth.Text)
        imgHeight = CDbl(Me.imgHeight.Text)
        lt.SetOption("View Update", 0)
        lt.Begin()
        Call createCamera(fov, lookD, arW, arH)
        Call createImgPlane(imgWidth, imgHeight, xNum, yNum, workDist, apSize)
        lt.Cmd("Select " + newCamName)
        lt.Cmd("More " + newDummyPlaneName)
        lt.Cmd("Group")
        lt.SetOption("View Update", 1)
        newGroupKey = "LENS_MANAGER[1].COMPONENTS[Components].GROUP[@Last]"
        newGroupName = lt.DbGet(newGroupKey, "Name")

        lt.DbSet(newGroupKey, "Name", "PinHoleCam")
        lt.End()
    End Sub

    Sub TwoDArrayToCSV(ByVal DataArray(,) As Double)
        Dim strTmp As String = ""
        Dim ofile As String = ""

        svDialog("csv|*.csv", "save as...", ofile)
        Dim sw As System.IO.StreamWriter = New System.IO.StreamWriter(ofile)

        For i As Int32 = DataArray.GetLowerBound(0) To DataArray.GetUpperBound(0)
            For j As Int32 = DataArray.GetLowerBound(1) To DataArray.GetUpperBound(1)
                strTmp += Str(DataArray(i, j)) + ","
            Next
            sw.WriteLine(strTmp)
            strTmp = ""
        Next
        sw.Flush()
        sw.Close()
    End Sub

    Sub svDialog(ByVal infilter As String, ByVal dtitle As String, ByRef outfile As String)
        Dim openFileDialog1 As New Microsoft.Win32.SaveFileDialog()
        With openFileDialog1
            .Filter = infilter
            .FilterIndex = 1
            .Title = dtitle
            .DefaultExt = Strings.Right(infilter, 3)
            .ShowDialog()
            outfile = openFileDialog1.FileName
            .RestoreDirectory = True
        End With
    End Sub
End Class

