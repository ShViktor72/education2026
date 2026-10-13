import express from "express";
import productRoutes from "./routes/productRoutes.js";

const app = express();

// ----------------------------------------------------
// 1. Middleware-логгер
// ----------------------------------------------------
app.use((req, res, next) => {
    console.log(req.method, req.url);
    // Передаём запрос дальше
    next();
});


// ----------------------------------------------------
// 2. Middleware проверки API-ключа
// ----------------------------------------------------
app.use((req, res, next) => {
    // Получаем ключ из заголовка запроса
    const apiKey = req.headers["x-api-key"];

    // Если ключ неправильный
    if (apiKey !== "12345") {
        // Завершаем запрос
        return res.status(401).json({
            message: "Invalid API key"
        });
    }
    // Если ключ правильный —
    // передаём запрос дальше
    next();
});


// ----------------------------------------------------
// Обычный маршрут
// ----------------------------------------------------
// Все запросы, начинающиеся с /api/products,
// передаём в productRoutes
app.use("/api/products", productRoutes);


app.listen(3000, () => {
    console.log("Server started: http://localhost:3000");
});