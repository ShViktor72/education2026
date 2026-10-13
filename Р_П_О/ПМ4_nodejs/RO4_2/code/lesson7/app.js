import express from "express";

import productRoutes from "./routes/productRoutes.js";
import { logger } from "./middleware/logger.js";

const app = express();


// Разбираем JSON-тело запроса
app.use(express.json());


// Подключаем middleware-логгер
app.use(logger);


// Все запросы /api/products...
// передаём в productRoutes
app.use("/api/products", productRoutes);


app.listen(3000, () => {
    console.log("Server started: http://localhost:3000");
});