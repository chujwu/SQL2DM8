@echo off
echo ========================================
echo 重启 SQL Server to DM8 Converter
echo ========================================

echo.
echo [1/4] 停止后端进程...
taskkill /F /IM SQLServerToDM8.exe 2>nul
timeout /t 2 /nobreak >nul

echo [2/4] 重新构建后端...
cd backend
dotnet build
if %errorlevel% neq 0 (
    echo 后端构建失败！
    pause
    exit /b 1
)
cd ..

echo [3/4] 启动后端...
start "SQL2DM8-Backend" cmd /c "cd backend && dotnet run"
timeout /t 3 /nobreak >nul

echo [4/4] 启动前端...
start "SQL2DM8-Frontend" cmd /c "cd frontend && npm run dev"

echo.
echo ========================================
echo 启动完成！
echo.
echo 后端: http://localhost:5000
echo 前端: http://localhost:5173
echo Swagger: http://localhost:5000/swagger
echo.
echo 请等待几秒钟让服务完全启动
echo ========================================
pause
