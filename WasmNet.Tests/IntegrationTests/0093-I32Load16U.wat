;; invoke: load16
;; expect: (i32:65494)

(module
    (memory 1)
    (func (export "load16") (result i32)
        i32.const 0 ;; dynamic offset
        i32.const -42 ;; 0b11111111111111111111111111010110
        i32.store offset=64 ;; store 32 bits at offset 64
        i32.const 0 ;; dynamic offset
        i32.load16_u offset=64 ;; load 16 bits at offset 64, zero-extend to 32 bits
    )
)