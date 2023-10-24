;; invoke: store-load
;; expect: (i32:42)

(module
    (memory 1)
    (func (export "store-load") (result i32)
        i32.const 0 ;; dynamic offset
        i32.const 42
        i32.store offset=64
        i32.const 0 ;; dynamic offset
        i32.load offset=64
    )
)