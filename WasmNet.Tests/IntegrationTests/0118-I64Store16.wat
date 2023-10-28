;; invoke: store16
;; expect: (i64:42)

(module
    (memory 1)
    (func (export "store16") (result i64)
        i32.const 0 ;; dynamic offset
        i64.const 196650 ;; 0b110000000000101010
        i64.store16 offset=64 ;; store 16 bits at offset 64
        i32.const 0 ;; dynamic offset
        i64.load offset=64
        ;; stack contains 0b101010 (42)
    )
)