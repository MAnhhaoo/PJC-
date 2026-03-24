1. Mô tả tổng quan
Đồ án xây dựng một hệ sinh thái kỹ thuật số nhằm quảng bá khu ẩm thực Vĩnh Khánh. Hệ thống hỗ trợ khách du lịch tìm kiếm nhà hàng, trải nghiệm thuyết minh đa ngôn ngữ và hỗ trợ các chủ nhà hàng số hóa hoạt động kinh doanh thông qua mô hình tài khoản phân cấp (Normal/Premium).

Hệ thống gồm 2 nền tảng:

Website Admin (ASP.NET Core MVC/Blazor): Quản trị toàn bộ dữ liệu hệ thống.

Mobile App (.NET MAUI): Dành cho cả Khách hàng và Chủ nhà hàng.

2. Mô hình phân quyền và Gói dịch vụ
2.1 Đối với Khách hàng (User)
Tài khoản Free: Xem danh sách và vị trí các nhà hàng cơ bản.

Tài khoản VIP: Mở khóa toàn bộ dữ liệu, bao gồm các nhà hàng Premium và ưu tiên các tính năng trải nghiệm cao cấp.

2.2 Đối với Nhà hàng (Restaurant Owner)
Gói Normal: Hiển thị thông tin cơ bản (Tên, địa chỉ, món ăn).

Gói Premium: Được ưu tiên hiển thị đầu danh sách, hỗ trợ hệ thống thuyết minh đa ngôn ngữ (Audio Narration) để thu hút khách quốc tế.

3. Các chức năng đã thực hiện
3.1 Chức năng Mobile App (Khách hàng & Nhà hàng)
Hệ thống Thuyết minh đa ngôn ngữ (Mới hoàn thiện): * Tích hợp phát Audio trực tiếp bên trong ứng dụng bằng IAudioManager.

Hỗ trợ chuyển đổi linh hoạt giữa các ngôn ngữ (Tiếng Việt, Tiếng Anh...).

Tự động chuyển sang chế độ Text-to-Speech (TTS) nếu nhà hàng chưa cập nhật file âm thanh.

Bản đồ & Chỉ đường (Nâng cao):

Hiển thị vị trí tương tác giữa người dùng và nhà hàng trên Google Maps.

Sử dụng OpenRouteService để tính toán khoảng cách, thời gian di chuyển thực tế và vẽ đường đi (Polyline) chi tiết.

Nút tắt mở nhanh Google Maps Navigation để dẫn đường bằng giọng nói.

Quản lý Tài khoản & Định danh:

Đăng ký/Đăng nhập bảo mật bằng JWT (JSON Web Token).

Phân luồng giao diện dựa trên Role (Khách hàng thấy danh sách, Nhà hàng thấy Dashboard quản lý).

Chỉnh sửa hồ sơ cá nhân và cập nhật ảnh đại diện.

Quản lý Nhà hàng (Dành cho chủ sở hữu):

Đăng ký thông tin nhà hàng mới vào hệ thống.

Quản lý danh mục món ăn (Thêm/Sửa/Xóa món).

Upload và quản lý file âm thanh thuyết minh cho từng ngôn ngữ.

Nâng cấp gói Premium để nhận ưu đãi hiển thị.

3.2 Chức năng Web Admin
Dashboard: Thống kê số lượng nhà hàng, người dùng và doanh thu gói Premium.

Quản lý thực thể: Kiểm soát dữ liệu nhà hàng và danh sách người dùng.

Phê duyệt hệ thống: Tiếp nhận yêu cầu đăng ký của nhà hàng mới và duyệt hiển thị trên App.

4. Công nghệ sử dụng
4.1 Backend (Hệ thống lõi)
Ngôn ngữ: C# (.NET 8/9).

Framework: ASP.NET Core Web API.

Database: SQL Server + Entity Framework Core (Code First).

Bảo mật: JWT Authentication, phân quyền Role-based.

Lưu trữ: File hệ thống (Static Files) cho hình ảnh và âm thanh thuyết minh.

4.2 Frontend Mobile
Công nghệ: .NET MAUI (Multi-platform App UI).

Thư viện hỗ trợ:

CommunityToolkit.Maui: Xử lý các UI component nâng cao.

Plugin.Maui.Audio: Phát âm thanh thuyết minh trực tiếp.

Maui.Maps: Tích hợp Google Maps native.

Newtonsoft.Json: Xử lý dữ liệu API.

OpenRouterService : Dùng để thể hiện Google Maps 

5. Các mục tiêu tiếp theo (Roadmap)
Hệ thống Đánh giá (Review): Cho phép người dùng chấm điểm và bình luận bằng hình ảnh.

Thông báo (Push Notification): Gửi thông báo cho Admin khi có nhà hàng mới đăng ký và thông báo cho User khi có khuyến mãi từ nhà hàng Premium.

Hệ thống thanh toán: Tích hợp cổng thanh toán (Momo/VNPAY) để tự động hóa việc nâng cấp tài khoản VIP và Premium.