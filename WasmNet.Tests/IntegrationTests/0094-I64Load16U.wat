;; invoke: load16
;; expect: (i64:65494)

(module
    (memory 1)
    (func (export "load16") (result i64)
        i32.const 0 ;; dynamic offset
        i64.const -42 ;; too many bits to list here
        i64.store offset=64 ;; store 32 bits at offset 64
        i32.const 0 ;; dynamic offset
        i64.load16_u offset=64 ;; load 16 bits at offset 64, zero-extend to 64 bits
    )
)