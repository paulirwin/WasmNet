;; invoke: leu
;; expect: (i32:1)

(module
    (func (export "leu") (result i32)
        i32.const 42
        i32.const 42
        i32.le_u
    )
)