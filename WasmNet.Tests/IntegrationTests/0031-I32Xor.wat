;; invoke: xor
;; expect: (i32:962)

(module
    (func (export "xor") (result i32)
        i32.const 42
        i32.const 1000
        i32.xor
    )
)