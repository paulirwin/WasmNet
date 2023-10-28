;; invoke: store32
;; expect: (i64:42)

(module
    (memory 1)
    (func (export "store32") (result i64)
        i32.const 0 ;; dynamic offset
        i64.const 12884901930 ;; 0b1100000000000000000000000000101010
        i64.store32 offset=64 ;; store 32 bits at offset 64
        i32.const 0 ;; dynamic offset
        i64.load offset=64
        ;; stack contains 0b101010 (42)
    )
)