import express from "express";
import productRoutes from "./routes/productRoutes.js";

const app = express();

app.use("/api/products", productRoutes);

app.listen(3000, () => {
    console.log("Server started: http://localhost:3000");
});

/**
Route
↓
Какой URL и HTTP-метод?

Controller
↓
Что взять из req и что отправить через res?

Service
↓
Что сделать с данными?
 */