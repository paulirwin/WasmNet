;; invoke: leu
;; expect: (i32:1)

(module
    (func (export "leu") (result i32)
        i64.const 42
        i64.const 42
        i64.le_u
    )
)