if(NOT TARGET OpenCV::opencv_java4)
add_library(OpenCV::opencv_java4 SHARED IMPORTED)
set_target_properties(OpenCV::opencv_java4 PROPERTIES
    IMPORTED_LOCATION "C:/Users/yesol/.gradle/caches/8.13/transforms/20bbafe0012d62af5d1e2afbb2fd725f/transformed/jetified-opencv-4.12.0/prefab/modules/opencv_java4/libs/android.arm64-v8a/libopencv_java4.so"
    INTERFACE_INCLUDE_DIRECTORIES "C:/Users/yesol/.gradle/caches/8.13/transforms/20bbafe0012d62af5d1e2afbb2fd725f/transformed/jetified-opencv-4.12.0/prefab/modules/opencv_java4/include"
    INTERFACE_LINK_LIBRARIES ""
)
endif()

