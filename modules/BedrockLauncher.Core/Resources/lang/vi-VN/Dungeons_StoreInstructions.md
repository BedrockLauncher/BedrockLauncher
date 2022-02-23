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

3. You will be asked to select a folder. Choose an empty folder where you want the game to be moved to. Do not choose a folder in Program Files or One Drive, it will break things.
4. The game will open at one point. Do not close it when this happens. If you run into any issues, make sure to check the Troubleshooting section below.
5. A `~mods` folder will appear. This is where you place your mods.
7. Launching the modded game is just like launching the regular game. You can do it from the start menu, Windows Store, Xbox app, and so on, just like you normally do. Do NOT try to launch it by running the .exe files in the game folder.

## Khắc phục sự cố:
- If you run into any issues while/after patching the game, some of these things might help you.
- If the game didn't open at all while patching and the patching didn't work, try opening the game manually before running the patcher. Keep the game open until it either closes by itself or the patcher finishes.
- If you get an error saying it can't verify your ownership of the game, you must have launched the game using the .exe file. Don't do that. Run the game from the start menu, Windows Store, or Xbox app. If you did, but still get this error, reinstall the regular game and log in at least once (open the game and select a character) before patching it.

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



