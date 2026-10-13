import express from "express";
import { getProducts } from "../controllers/productController.js";

const router = express.Router();


// Route только связывает URL с controller

router.get("/", getProducts);


export default router;