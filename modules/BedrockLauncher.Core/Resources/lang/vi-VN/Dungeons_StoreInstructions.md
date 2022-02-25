# Phần 1 - Tập lệnh Powershell
Phiên bản Windows Store của Minecraft Dungeons thường không cho phép bạn sửa đổi các tệp / thư mục của nó. Để khắc phục sự cố này, hãy làm theo các bước sau:

## Điều kiện tiên quyết:
- Tạm thời vô hiệu hóa bất kỳ phần mềm chống vi-rút. Hầu hết sẽ phát hiện ra rằng một tập lệnh không xác định đang được chạy và cố gắng ngăn chặn nó.
- Nếu bạn sử dụng Bitdefender, bạn cần gỡ cài đặt nó trước khi tiếp tục, vì nó sẽ phá vỡ tập lệnh ngay cả khi đã tắt.
- Đảm bảo bạn có ít nhất 10 GB dung lượng trống.
- Đảm bảo rằng trò chơi của bạn được cập nhật. Để thực hiện việc này, hãy nhấn Win + R, nhập `ms-windows-store://DownloadsAndUpdates` và nhấn enter. Sau đó, nhấn "Nhận bản cập nhật" ở góc trên cùng bên phải của cửa sổ mở ra.
- Cài đặt [Visual C++ Redist](https://aka.ms/vs/16/release/vc_redist.x64.exe). Ngay cả khi bạn nghĩ rằng bạn đã cài đặt nó, hãy thử trình cài đặt. Bạn có thể có phiên bản cũ hơn không hoạt động.

## Trong trình khởi chạy Bedrock:
1. Đảm bảo rằng biến thể trò chơi của bạn được đặt thành `Microsoft Store`
3. Nhấp vào `Cập nhật bản vá cửa hàng`

## Trong cửa sổ Powershell:

3. Bạn sẽ được yêu cầu chọn một thư mục. Chọn một thư mục trống nơi bạn muốn trò chơi được chuyển đến. Đừng chọn một thư mục trong Program Files hoặc One Drive, nó sẽ phá vỡ mọi thứ.
4. Trò chơi sẽ mở tại một thời điểm. Đừng đóng nó khi điều này xảy ra. Nếu bạn gặp phải bất kỳ sự cố nào, hãy nhớ kiểm tra phần Khắc phục sự cố bên dưới.
5. Thư mục `~mods` sẽ xuất hiện. Đây là nơi bạn đặt "mod" của mình.
7. Khởi chạy trò chơi đã mod cũng giống như khởi chạy trò chơi thông thường. Bạn có thể làm điều đó từ Start menu, Windows Store, ứng dụng Xbox,.v.v. giống như bạn thường làm. KHÔNG cố khởi chạy nó bằng cách chạy các tệp .exe trong thư mục trò chơi.

## Khắc phục sự cố:
- Nếu bạn gặp phải bất kỳ sự cố nào trong khi/sau khi vá trò chơi, một số điều sau đây có thể giúp ích cho bạn.
- Nếu trò chơi hoàn toàn không mở trong khi vá và quá trình vá không hoạt động, hãy thử mở trò chơi theo cách thủ công trước khi chạy trình vá. Giữ trò chơi mở cho đến khi nó tự đóng lại hoặc bản vá kết thúc.
- Nếu bạn gặp lỗi thông báo rằng nó không thể xác minh quyền sở hữu trò chơi của bạn, bạn phải khởi chạy trò chơi bằng tệp .exe. Đừng làm vậy. Chạy trò chơi từ Start menu, Windows Store hoặc ứng dụng Xbox. Nếu bạn đã làm, nhưng vẫn gặp lỗi này, hãy cài đặt lại trò chơi thông thường và đăng nhập ít nhất một lần (mở trò chơi và chọn nhân vật) trước khi vá lỗi.

# Phần 2 - Thiết lập Trình khởi chạy Bedrock
1. Đặt vị trí cài đặt vào thư mục chứa ` Dungeons.exe `. Nó phải nằm trong thư mục mẹ của thư mục ` ~ mods ` trước đó
2. Chọn nơi bạn muốn thư mục "mods" tượng trưng của mình (nó không được ở cùng vị trí với thư mục ` ~ mods ` trước đó)
3. Bấm vào `Cài đặt liên kết tượng trưng`
4. Thư mục "mod" trong trò chơi của bạn bây giờ sẽ được ghép nối với thư mục "mod" tượng trưng của bạn

# Cách cập nhật
1. Bấm vào `Cài đặt liên kết tượng trưng`
2. Nhấp vào `Cập nhật bản vá cửa hàng`
3. Lặp lại các bước từ 3 đến 6 của Phần 1 nếu cần
4. Lặp lại các bước từ 1 đến 4 của Phần 2
5. Xong



