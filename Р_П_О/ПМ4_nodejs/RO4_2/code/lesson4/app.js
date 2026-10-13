import express from "express";
import productRoutes from "./routes/productRoutes.js";

const app = express();


// Все запросы, начинающиеся с /api/products,
// передаём в productRoutes

app.use("/api/products", productRoutes);


app.listen(3000, () => {
    console.log("Server started: http://localhost:3000");
});