Imports LightTools
Imports LTLOCATORLib

Class MainWindow
    Dim ltLoc As New Locator
    Dim lt As LTAPI
    Dim js As New LTCOM64.JSNET
    Dim pID As Integer
    Dim fov, lookD, arW, arH As Double
    Dim imgWidth, imgHeight, xNum, yNum, workDist, apSize As Double
    Sub New()
        ' 設計工具需要此呼叫。
        InitializeComponent()
        pID = 8432
        lt = ltLoc.GetLTAPI(pID)
        xNum = 320
        yNum = 200
        workDist = 20
        apSize = 0.01
        imgHeight = 2 * workDist * Math.Tan((fov * Math.PI / 180) / 2)
        imgWidth = imgHeight * arW / arH
        ' 在 InitializeComponent() 呼叫之後加入所有初始設定。
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
        'Private Sub createCamera()
        Dim newCamKey As String
        lt.Cmd("PlaceCamera " + js.LTCoord3(0, 0, 0) + " " + js.LTCoord3(0, 0, 1)) 'create a camera at origin
        newCamKey = "LENS_MANAGER[1].STUDIO_MANAGER[Studio_Manager].DATABASE[Camera_List].ENTITY[@Last]"
        lt.DbSet(newCamKey, "Field_Of_View", fov)
        lt.DbSet(newCamKey, "Look_Distance", lookDist)
        lt.DbSet(newCamKey, "Aspect_Width", arW)
        lt.DbSet(newCamKey, "Aspect_Height", arH)
    End Sub
    Private Sub createImgPlane(width As Double, height As Double, xNum As Integer, yNum As Integer, workDist As Double, apSize As Double)
        Dim newDummyPlaneKey As String
        Dim newReceiverKey As String
        Dim newLumMeshKey As String
        js.MakeDummyPlane(lt, 0, 0, -workDist, 0, 0, 1, W:=width, H:=height)
        newDummyPlaneKey = "LENS_MANAGER[1].COMPONENTS[Components].PLANE_DUMMY_SURFACE[@Last]"
        js.MakeReceiver(lt, SurfaceName:=newDummyPlaneKey, ReceiverName:="Pinhole Image")
        newReceiverKey = "LENS_MANAGER[1].ILLUM_MANAGER[Illumination_Manager].RECEIVERS[Receiver_List].SURFACE_RECEIVER[@Last]"
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "VirtualAperture", "Yes")
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "Distance", workDist)
        lt.DbSet(newReceiverKey + ".SPATIAL_LUM_METER[Spatial_Lum_Meter]", "Disk_Radius", apSize)
        lt.DbSet(newReceiverKey + ".BACKWARD[Backward_Simulation]", "Has_Spatial_Luminance", "Yes")
        newLumMeshKey = newReceiverKey + ".BACKWARD_SPATIAL_LUMINANCE[Spatial_Luminance].SPATIAL_LUMINANCE_MESH[Spatial_Luminance_Mesh]"
        lt.DbSet(newLumMeshKey, "X_Dimension", xNum)
        lt.DbSet(newLumMeshKey, "Y_Dimension", yNum)
    End Sub

    Private Sub button_Click(sender As Object, e As RoutedEventArgs) Handles button.Click
        Call ltTest()
        'Call createCamera(50, 10, 4, 3)
        'Call createImgPlane(imgWidth, imgHeight, xNum, yNum, workDist, apSize)
    End Sub
End Class
