;; invoke: load32
;; expect: (i64:-42)

(module
    (memory 1)
    (func (export "load32") (result i64)
        i32.const 0 ;; dynamic offset
        i64.const -42 ;; 0b11111111111111111111111111010110 sign-extended to 64 bits
        i64.store offset=64 ;; store 64 bits at offset 64
        i32.const 0 ;; dynamic offset
        i64.load32_s offset=64 ;; load 32 bits at offset 64, sign-extend to 64 bits
    )
)