# 🤖 Robotic Glazing System (Hệ Thống Robot Tra Keo Tự Động)

Đây là phần mềm điều khiển trung tâm (PC-based Control Software) cho hệ thống tự động bôi keo (Glazing) và lắp ráp kính công nghiệp. Phần mềm điều phối nhịp nhàng giữa Camera AI, PLC Siemens và nhiều Cánh tay Robot ABB.

## 🌟 Chức năng chính

1. **Hiển thị giao diện người dùng (HMI) bằng WinForms**: Giao diện điều khiển trực quan giúp kiểm soát toàn bộ chu trình hệ thống.
2. **Thị giác máy tính (Computer Vision) với YOLO**: Khởi chạy liên tục Camera và chạy mô hình ONNX để phát hiện và phân loại kính (`gnGlassType`). Ghi nhận lịch sử kèm tỉ lệ phần trăm độ chính xác.
3. **Giao tiếp PLC công nghiệp (Siemens)**: Nhận phản hồi tín hiệu I/O thông qua chuẩn S7, ra lệnh đóng/mở tự động Van 5/2 (Van nhả keo) và các tín hiệu tay gắp đồng bộ theo quỹ đạo di chuyển của Robot.
4. **Điều phối Robot ABB (ABB RobotStudio API)**: Giao tiếp trực tiếp với Bộ điều khiển ABB qua mạng LAN. Gửi lệnh (`HOME`, `SCAN`, `WAIT_GLUE`, `GLUE_REAR`, `GLUE_SIDE`) và đọc biến ghi/nhận từ bộ điều khiển.

## 🛠 Công nghệ sử dụng
- **Ngôn ngữ**: C# / .NET 9.0 (Windows)
- **Giao diện**: Windows Forms
- **Thị giác Máy tính**: `OpenCvSharp4`, `Microsoft.ML.OnnxRuntime`
- **Giao tiếp PLC**: Thư viện `S7netplus` (S7-1200 / S7-1500)
- **Hệ điều khiển Robot**: Bộ thư viện `ABB.Robotics.Controllers` đi kèm RobotStudio 2025.

## ⚙️ Hướng dẫn cài đặt & Chạy ứng dụng
1. Clone dự án về máy:
   ```bash
   git clone https://github.com/zablee-dev/RoboticGlazingSystem.git
   ```
2. Mở file `RoboticGlazingSystem.WinForms.csproj` bằng Visual Studio 2022 trở lên.
3. Cài đặt các thiếu sót liên quan đến thư viện từ NuGet Packages.
   *(Lưu ý: Bạn phải Cài đặt [ABB RobotStudio 2025](https://new.abb.com/products/robotics/robotstudio) trong máy tính để có thể nạp các file DLL ABB Robotics hợp lệ)*
4. Nhấn **F5** để bắt đầu ứng dụng.

## 🔄 Tổng quan luồng Chu Trình Tự Động (Auto Cycle)
Khi nhấn nút **▶ AUTO RUN**, hệ thống bắt đầu chu trình khép kín:
1. Xác nhận các kết nối Robot Tự động, Khởi động PLC, ra lệnh Robot về Home (Gốc).
2. Khi có tín hiệu Start vật lý (từ I0.0), bật Camera.
3. Chạy `YoloOnnx` phát hiện kính. Lưu dữ liệu phân loại báo về Robot (Gửi Lệnh SCAN).
4. PC theo dõi trạng thái `Robot A` vào điểm nhả keo (I0.6).
5. Gửi lệnh thực thi nhả Keo `GLUE` tới `Robot B`.
6. Tự động bật Van trét keo liên tục cho khi `Robot B` báo kết thúc (`gbGlueDone = True`). Báo cho `Robot A` lắp kính.
7. Khi hoàn tất, Reset lại tín hiệu toàn cục, báo tín hiệu cho Bắt đầu một tiến trình tự động vòng lặp tiếp theo.
