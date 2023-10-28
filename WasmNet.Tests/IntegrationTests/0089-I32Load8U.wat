;; invoke: load8
;; expect: (i32:214)

(module
    (memory 1)
    (func (export "load8") (result i32)
        i32.const 0 ;; dynamic offset
        i32.const -42 ;; 0b11111111111111111111111111010110
        i32.store offset=64 ;; store 32 bits at offset 64
        i32.const 0 ;; dynamic offset
        i32.load8_u offset=64 ;; load 8 bits at offset 64, zero-extended to 32 bits
        ;; stack contains 0b11010110 (214) at this point
    )
)