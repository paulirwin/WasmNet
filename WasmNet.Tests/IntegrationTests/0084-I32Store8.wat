;; invoke: store8
;; expect: (i32:42)

(module
    (memory 1)
    (func (export "store8") (result i32)
        i32.const 0 ;; dynamic offset
        i32.const 810 ;; 0b1100101010
        i32.store8 offset=64 ;; store 8 bits at offset 64
        i32.const 0 ;; dynamic offset
        i32.load offset=64
        ;; stack contains 0b101010 (42)
    )
)