;; invoke: life
;; expect: (i32:42)

(module
    (func (export "life") (result i32)
        (i32.const 42)
    )
)