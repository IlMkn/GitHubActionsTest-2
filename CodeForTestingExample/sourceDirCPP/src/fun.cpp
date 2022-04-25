// Copyright 2022 UNN-IASR
#include "fun.h"

int64_t power(int64_t x, uint16_t n){
    int result = 0;
    if (n == 0)
        result = 1;
    else{
        result = 1;
        for (int i = 0; i < n; i++){
            result = result * x;
        }
    }
    return result;
}
