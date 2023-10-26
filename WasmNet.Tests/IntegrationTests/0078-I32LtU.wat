;; invoke: ltu
;; expect: (i32:1)

(module
    (memory 1)
    (func (export "ltu") (result i32)
        i32.const 10
        i32.const 42
        i32.lt_u
    )
)