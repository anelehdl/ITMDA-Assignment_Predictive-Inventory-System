//ApiService.java
//        Defines the HTTP request I want to make
//        Endpoint actions are located here.
//        Group Members: Charlene Higgo, Armand Geldenhuys, Aneleh de Lange, Travis Musson, Grant Peterson, Petrus
//        2025
//        Developed for NuTec Inks

package com.eduvos.nutec.api;

import com.eduvos.nutec.pojo.CreateOrderRequest;
import com.eduvos.nutec.pojo.OrderResponse;
import com.eduvos.nutec.pojo.OrdersListResponse;
import com.google.gson.JsonObject;

import java.util.List;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.http.Body;
import retrofit2.http.GET;
import retrofit2.http.POST;
import retrofit2.http.Path;

import com.eduvos.nutec.pojo.LoginResponse;
import com.eduvos.nutec.pojo.LoginRequest;
import com.eduvos.nutec.pojo.ProductOrder;
import com.eduvos.nutec.pojo.OrderPayload;

public interface ApiService {

    //testing connection
    @GET("Test/ping")
    Call<PingResponse> ping();


    @POST("auth/login")
    Call<LoginResponse> login(@Body LoginRequest loginRequest);
//    // Get products
//    @GET("products")
//    Call<List<ProductOrder>> getProductOrders();

    //correct api call for inventory (also created inventory dto without the getters and setters because I  am unsure about how to do that in java)
    @GET("StockMetrics/inventory")
    Call<List<ProductOrder>> getProductOrders();


    // order endpoints
    @POST("orders")
    Call<OrderResponse> createOrder(@Body CreateOrderRequest request);

    @GET("orders")
    Call<OrdersListResponse> getUserOrders();

    @GET("orders/{id}")
    Call<OrderResponse> getOrderById(@Path("id") String orderId);
}
