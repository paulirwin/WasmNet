;; invoke: load8
;; expect: (i64:214)

(module
    (memory 1)
    (func (export "load8") (result i64)
        i32.const 0 ;; dynamic offset
        i64.const -42 ;; too many bits to list here
        i64.store offset=64 ;; store 64 bits at offset 64
        i32.const 0 ;; dynamic offset
        i64.load8_u offset=64 ;; load 8 bits at offset 64, zero-extended to 64 bits
        ;; stack contains 0b11010110 (214)
    )
)