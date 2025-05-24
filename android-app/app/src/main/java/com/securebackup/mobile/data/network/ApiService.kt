package com.securebackup.mobile.data.network

import com.securebackup.mobile.data.model.*
import okhttp3.MultipartBody
import okhttp3.RequestBody
import retrofit2.Response
import retrofit2.http.*

interface ApiService {
    
    @POST("auth/login")
    suspend fun login(@Body request: LoginRequest): Response<AuthResponse>
    
    @POST("auth/register")
    suspend fun register(@Body request: RegisterRequest): Response<AuthResponse>
    
    @GET("auth/google")
    suspend fun googleAuth(@Query("token") token: String): Response<AuthResponse>
    
    @GET("auth/user")
    suspend fun getCurrentUser(@Header("Authorization") token: String): Response<User>
    
    @GET("files")
    suspend fun getFiles(@Header("Authorization") token: String): Response<FilesResponse>
    
    @Multipart
    @POST("files/upload")
    suspend fun uploadFile(
        @Header("Authorization") token: String,
        @Part file: MultipartBody.Part,
        @Part("metadata") metadata: RequestBody
    ): Response<UploadResponse>
    
    @GET("files/download")
    suspend fun downloadFile(
        @Header("Authorization") token: String,
        @Query("key") s3Key: String
    ): Response<okhttp3.ResponseBody>
    
    @DELETE("files")
    suspend fun deleteFile(
        @Header("Authorization") token: String,
        @Query("key") s3Key: String
    ): Response<ApiResponse>
    
    @GET("files/stats")
    suspend fun getUserStats(@Header("Authorization") token: String): Response<UserStats>
}