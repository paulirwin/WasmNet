;; invoke: store-load
;; expect: (i64:4200000000)

(module
    (memory 1)
    (func (export "store-load") (result i64)
        i32.const 0 ;; dynamic offset
        i64.const 4200000000
        i64.store offset=128
        i32.const 0 ;; dynamic offset
        i64.load offset=128
    )
)