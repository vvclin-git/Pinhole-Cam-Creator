Imports LightTools
Imports LTLOCATORLib

Class MainWindow
    Dim ltLoc As New Locator
    Dim lt As LTAPI
    Dim js As New LTCOM64.JSNET
    Dim pID As Integer
    Dim stat As LTReturnCodeEnum
    Dim newDummyPlaneKey As String
    Dim newDummyPlaneName As String
    Dim newCamKey As String
    Dim newCamName As String
    Dim newGroupKey, newGroupName As String


    Sub New()
        ' 設計工具需要此呼叫。
        InitializeComponent()
        'pID = 10584
        'lt = ltLoc.GetLTAPI(pID)
        lt = LTGetter.getLTAPIServer
        If IsNothing(lt) Then
            MsgBox("LightTools session not found!")
        Else
            Me.Title = "Pinhole Camera Creator (PID = " + Str(lt.GetServerID()) + ")"
            initSettings()
        End If
    End Sub
    Private Sub ltTest()
        Dim stat, stat2 As LTReturnCodeEnum
        stat = lt.Cmd("\V3D")
        Debug.Print(stat)
        'lt.Cmd("PlaceCamera " + js.LTCoord3(0, 0, 0) + " " + js.LTCoord3(0, 0, 1))
        stat2 = lt.Cmd("PlaceCamera " + lt.Coord3(0, 0, 0) + " " + lt.Coord3(0, 5, 0))
        Debug.Print(stat2)
    End Sub
    Private Sub createCamera(fov As Double, lookDist As Double, arW As Double, arH As Double)
        'create PRR camera
        lt.Cmd("\V3D")
        lt.Cmd("PlaceCamera " + js.LTCoord3(0, 0, 0) + " " + js.LTCoord3(0, 0, 1)) 'create a camera at origin
        newCamKey = "LENS_MANAGER[1].STUDIO_MANAGER[Studio_Manager].DATABASE[Camera_List].ENTITY[@Last]"
        newCamName = lt.DbGet(newCamKey, "Name")
        lt.DbSet(newCamKey, "Field_Of_View", fov)
        lt.DbSet(newCamKey, "Look_Distance", lookDist)
        lt.DbSet(newCamKey, "Aspect_Width", arW)
        lt.DbSet(newCamKey, "Aspect_Height", arH)
    End Sub
    Private Sub createImgPlane(width As Double, height As Double, xNum As Integer, yNum As Integer, workDist As Double, apSize As Double)
        'create image plane for pin hole imaging
        Dim newReceiverKey As String
        Dim newLumMeshKey As String
        stat = js.MakeDummyPlane(lt, 0, 0, -workDist, 0, 0, 1, W:=width, H:=height, ApertureType:="Rectangular") 'create dummy plane
        Debug.Print("dummy plane creating status: " + Str(stat))
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
        'luminance mesh settings
        newLumMeshKey = newReceiverKey + ".BACKWARD_SPATIAL_LUMINANCE[Spatial_Luminance].SPATIAL_LUMINANCE_MESH[Spatial_Luminance_Mesh]"
        lt.DbSet(newLumMeshKey, "X_Dimension", xNum)
        lt.DbSet(newLumMeshKey, "Y_Dimension", yNum)
        lt.DbSet(newLumMeshKey, "Name", "PinHoleLumMap")
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

    Private Sub arW_TextChanged(sender As Object, e As TextChangedEventArgs) Handles arW.TextChanged
        If Me.IsInitialized Then
            If IsNumeric(DirectCast(sender, TextBox).Text) Then
                calcDimension()
            End If
        End If
    End Sub

    Private Sub arH_TextChanged(sender As Object, e As TextChangedEventArgs) Handles arH.TextChanged
        If Me.IsInitialized Then
            If IsNumeric(DirectCast(sender, TextBox).Text) Then
                calcDimension()
            End If
        End If
    End Sub

    Private Sub workDist_TextChanged(sender As Object, e As TextChangedEventArgs) Handles workDist.TextChanged
        If Me.IsInitialized Then
            If IsNumeric(DirectCast(sender, TextBox).Text) Then
                calcDimension()
            End If
        End If
    End Sub
    Private Sub fov_TextChanged(sender As Object, e As TextChangedEventArgs) Handles fov.TextChanged
        If Me.IsInitialized Then
            If IsNumeric(DirectCast(sender, TextBox).Text) Then
                calcDimension()
            End If
        End If
    End Sub
    Private Sub initSettings()
        If Me.IsInitialized Then
            'Luminance mesh settings
            Me.xNum.Text = 320 'number of bins in x-direction
            Me.yNum.Text = 200 'number of bins in y-direction
            Me.workDist.Text = 20 'working distance
            Me.apSize.Text = 0.02 'aperture size
            Me.imgHeight.Text = 18.6523
            Me.imgWidth.Text = 24.8697
            'PRR camera settings
            Me.fov.Text = 50 'FoV
            Me.lookD.Text = 10 'looking distance
            Me.arW.Text = 4 'aspect ratio (width)
            Me.arH.Text = 3 'aspect raito (height)
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



    Private Sub yNum_KeyUp(sender As Object, e As KeyEventArgs) Handles yNum.KeyUp
        Dim input As Integer
        If Not Integer.TryParse(DirectCast(sender, TextBox).Text, input) Then
            MsgBox("Integer value is required in this field!")
            DirectCast(sender, TextBox).Undo()
        End If
    End Sub

    Private Sub xNum_KeyUp(sender As Object, e As KeyEventArgs) Handles xNum.KeyUp
        Dim input As Integer
        If Not Integer.TryParse(DirectCast(sender, TextBox).Text, input) Then
            MsgBox("Integer value is required in this field!")
            DirectCast(sender, TextBox).Undo()
        End If
    End Sub
End Class
