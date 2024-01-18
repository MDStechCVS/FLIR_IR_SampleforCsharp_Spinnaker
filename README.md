<img width="320" alt="mds" src="https://github.com/MDStechCVS/FLIR_IR_SampleforCsharp_Spinnaker/assets/142575573/d301dcfe-14c9-4ec5-91e7-9ee2b01314b2">

# FLIR_IR_SampleforCsharp_Spinnaker
<img width="527" alt="image" src="https://github.com/MDStechCVS/FLIR_IR_SampleforCsharp_Spinnaker/assets/142575573/c182c5ee-7995-4a28-9cad-5ca0869c3489">

FLIR IR 카메라 Ax5의 영상과 최대/최소 온도 값, 특정 영역(ROI)의 최대/최소 온도 값을 보여주는 예제입니다. 

 
This is an example showing images from FLIR IR cameras A6xx and Ax5, maximum and minimum temperature values, and temperature values of a specific area (ROI).

This is pure example code, and optimization and exception handling are up to the user.


## Requirement
FLIR IR Camera Ax5


Spinnaker SDK 3.1.0.97


Windows11


### [Spinnaker SDK 3.1.0.97 Download site ( FLIR Site (Login Only) )](https://www.flir.com/support-center/iis/machine-vision/downloads/spinnaker-sdk-download/spinnaker-sdk--download-files/)

<img width="581" alt="spinnakersdk" src="https://github.com/MDStechCVS/FLIR_IR_SampleforCsharp_Spinnaker/assets/142575573/be85ee22-84f6-4c31-b609-b0fdb73e8c18">

## Run
Spinnaker SDK를 다운로드 및 설치한 후 소스를 빌드합니다. 


Download the Spinnaker SDK from the site above, install it, and then build the source.


프로그램을 실행 후, 아래와 같이 연결하고자 하는 카메라를 선택하면 영상과 온도값이 출력됩니다. 


After running the program, select the camera you want to connect as shown below, and video and temperature values will be output.


<img width="700" alt="first" src="https://github.com/MDStechCVS/FLIR_IR_SampleforCsharp_Spinnaker/assets/142575573/3ca82c71-cdf1-4839-b7a9-fc3544ff0ff8">

<img width="700" alt="second" src="https://github.com/MDStechCVS/FLIR_IR_SampleforCsharp_Spinnaker/assets/142575573/78bde05b-aaa9-4c1b-8763-36d7376250cb">




Set ROI Box 버튼을 클릭하면 ROI 영역이 표시되고, 해당 영역안의 최대/최소 온도 값이 출력됩니다. 

![roibutton](https://github.com/MDStechCVS/FLIR_IR_SampleforCsharp_Spinnaker/assets/142575573/7deeac94-1b91-44db-bff4-e420296a541d)





## Contact
For bugs and inquiries, please contact us through GitHub Issues or team email.


team email : cvs_team@mdstech.co.kr


